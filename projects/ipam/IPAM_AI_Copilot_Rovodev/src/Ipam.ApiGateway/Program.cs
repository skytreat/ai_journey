using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Ipam.ApiGateway
{
    /// <summary>
    /// API Gateway entry point with authentication, authorization, and rate limiting
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add comprehensive logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddEventSourceLogger();

            // Add health checks for downstream services
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddUrlGroup(new Uri($"{builder.Configuration["FrontendServiceUrl"]}/health"), 
                    name: "frontend-service", 
                    failureStatus: HealthStatus.Degraded,
                    timeout: TimeSpan.FromSeconds(5));

            // Add resilient HTTP client with Polly
            builder.Services.AddHttpClient("frontend-client", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["FrontendServiceUrl"] ?? "https://localhost:5001");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Add JWT authentication with enhanced validation
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "")),
                    ClockSkew = TimeSpan.FromMinutes(1) // Reduce clock skew tolerance
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Authentication failed: {Exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("Token validated for user: {User}", context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    }
                };
            });

            // Add authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
            });

            // Add rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("api", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 100;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 50;
                });
            });

            var app = builder.Build();

            // Add request logging middleware
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var correlationId = Guid.NewGuid().ToString("N")[..8];
                context.Items["CorrelationId"] = correlationId;
                
                logger.LogInformation("Request started: {Method} {Path} [CorrelationId: {CorrelationId}]", 
                    context.Request.Method, context.Request.Path, correlationId);
                    
                await next();
                
                logger.LogInformation("Request completed: {Method} {Path} {StatusCode} [CorrelationId: {CorrelationId}]", 
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, correlationId);
            });

            // Use authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Use rate limiting
            app.UseRateLimiter();

            // Health checks endpoint
            app.MapHealthChecks("/health");

            // Map API routes with improved forwarding
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                    var correlationId = context.Items["CorrelationId"]?.ToString();
                    
                    try
                    {
                        var httpClient = httpClientFactory.CreateClient("frontend-client");
                        var path = context.Request.Path;
                        var queryString = context.Request.QueryString;
                        var targetUrl = $"{path}{queryString}";

                        // Create request message
                        var requestMessage = new HttpRequestMessage(
                            new HttpMethod(context.Request.Method), 
                            targetUrl);

                        // Copy headers (except host)
                        foreach (var header in context.Request.Headers)
                        {
                            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                            {
                                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                            }
                        }

                        // Add correlation ID
                        if (!string.IsNullOrEmpty(correlationId))
                        {
                            requestMessage.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
                        }

                        // Copy body for POST/PUT requests
                        if (context.Request.Method != "GET" && context.Request.Method != "HEAD")
                        {
                            requestMessage.Content = new StreamContent(context.Request.Body);
                            if (context.Request.ContentType != null)
                            {
                                requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
                            }
                        }

                        logger.LogDebug("Forwarding request to: {TargetUrl} [CorrelationId: {CorrelationId}]", 
                            targetUrl, correlationId);

                        // Forward the request
                        var response = await httpClient.SendAsync(requestMessage);
                        var content = await response.Content.ReadAsStringAsync();
                        
                        // Copy response headers
                        foreach (var header in response.Headers)
                        {
                            context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                        }
                        
                        context.Response.StatusCode = (int)response.StatusCode;
                        context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
                        
                        await context.Response.WriteAsync(content);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error forwarding request [CorrelationId: {CorrelationId}]", correlationId);
                        context.Response.StatusCode = 502; // Bad Gateway
                        await context.Response.WriteAsync($"{{\"error\":\"Gateway error\",\"correlationId\":\"{correlationId}\"}}");
                    }
                });
            });

            app.Run();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, duration) =>
                    {
                        Console.WriteLine($"Circuit breaker opened for {duration}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit breaker reset");
                    });
        }
    }
}
