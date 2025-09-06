using IPAM.Application;
using IPAM.Infrastructure;
using IPAM.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Services.Frontend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Storage
builder.Services.AddIpamTableStorage(o =>
{
	o.ConnectionString = builder.Configuration.GetValue<string>("TableStorage:ConnectionString") ?? "UseDevelopmentStorage=true";
});

var devAuthEnabled = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DevAuth:Enabled");

// JWT + RBAC (or Dev)
if (devAuthEnabled)
{
	builder.Services.AddAuthentication(DevAuthenticationHandler.Scheme)
		.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.Scheme, _ => { });
}
else
{
	builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
		{
			options.Authority = builder.Configuration["Auth:Authority"];
			options.Audience = builder.Configuration["Auth:Audience"];
			options.RequireHttpsMetadata = false;
		});
}

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("SystemAdmin", p => p.RequireClaim("role", "SystemAdmin"));
	options.AddPolicy("AddressSpaceAdmin", p => p.RequireAssertion(ctx =>
		ctx.User.HasClaim(c => c.Type == "role" && (c.Value == "SystemAdmin" || c.Value == "AddressSpaceAdmin"))));
	options.AddPolicy("AddressSpaceViewer", p => p.RequireAssertion(ctx =>
		ctx.User.HasClaim(c => c.Type == "role" && (c.Value == "SystemAdmin" || c.Value == "AddressSpaceAdmin" || c.Value == "AddressSpaceViewer"))));
});

// Telemetry
var otelBuilder = builder.Services.AddOpenTelemetry()
	.WithTracing(t => t
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddSource("IPAM")
		.AddConsoleExporter())
	.WithMetrics(m => m.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().AddConsoleExporter());

var aiConn = builder.Configuration["AzureMonitor:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConn))
{
	otelBuilder.UseAzureMonitor(o => { o.ConnectionString = aiConn; });
}

builder.Services.AddSingleton<ICidrService, BasicCidrService>();
builder.Services.AddSingleton<ITagPolicyService, TagPolicyService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
app.MapGet("/health", () => Results.Ok("ok")).AllowAnonymous();

app.Run();
