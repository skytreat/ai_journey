using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// Telemetry
var otelBuilder = builder.Services.AddOpenTelemetry()
	.WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddConsoleExporter())
	.WithMetrics(m => m.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().AddConsoleExporter());

var aiConn = builder.Configuration["AzureMonitor:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConn))
{
	otelBuilder.UseAzureMonitor(o => { o.ConnectionString = aiConn; });
}

// Simple IP allow/deny
var allowCidrs = builder.Configuration.GetSection("Security:IPAllow").Get<string[]>() ?? Array.Empty<string>();
var denyCidrs = builder.Configuration.GetSection("Security:IPDeny").Get<string[]>() ?? Array.Empty<string>();

// Rate limiting (fixed window per IP)
var permitLimit = builder.Configuration.GetValue<int?>("RateLimit:PermitLimit") ?? 100;
var windowSeconds = builder.Configuration.GetValue<int?>("RateLimit:WindowSeconds") ?? 60;
var queueLimit = builder.Configuration.GetValue<int?>("RateLimit:QueueLimit") ?? 0;

builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
	{
		var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
		{
			PermitLimit = permitLimit,
			Window = TimeSpan.FromSeconds(windowSeconds),
			QueueLimit = queueLimit,
			QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
		});
	});
});

builder.Services.AddReverseProxy().LoadFromMemory(new[]
{
	new Yarp.ReverseProxy.Configuration.RouteConfig
	{
		RouteId = "frontend",
		ClusterId = "frontend",
		Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/api/{**catch-all}" }
	}
}, new[]
{
	new Yarp.ReverseProxy.Configuration.ClusterConfig
	{
		ClusterId = "frontend",
		Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
		{
			["d1"] = new() { Address = builder.Configuration["Upstreams:Frontend"] ?? "http://localhost:5080" }
		}
	}
});

var app = builder.Build();

// IP allow/deny first
app.Use(async (ctx, next) =>
{
	var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "";
	bool denied = denyCidrs.Any(c => remoteIp.StartsWith(c, StringComparison.OrdinalIgnoreCase));
	bool allowed = allowCidrs.Length == 0 || allowCidrs.Any(c => remoteIp.StartsWith(c, StringComparison.OrdinalIgnoreCase));
	if (denied || !allowed)
	{
		ctx.Response.StatusCode = 403;
		await ctx.Response.WriteAsync("Forbidden");
		return;
	}
	await next();
});

// Then apply rate limiting
app.UseRateLimiter();

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
