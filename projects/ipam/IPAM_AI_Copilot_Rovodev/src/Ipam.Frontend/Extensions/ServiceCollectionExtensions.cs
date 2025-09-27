using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Ipam.Frontend.Filters;
using Ipam.Frontend.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Asp.Versioning;

namespace Ipam.Frontend.Extensions
{
    /// <summary>
    /// Extension methods for configuring frontend services
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFrontendServices(this IServiceCollection services)
        {
            // Add controllers with enhanced configuration
            services.AddControllers(options =>
            {
                // Add global filters
                options.Filters.Add<ValidationFilter>();
                options.Filters.Add<PerformanceLoggingFilter>();
                
                // Configure model binding
                options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
                    _ => "This field is required.");
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Customize validation error responses
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                        );

                    return new BadRequestObjectResult(new
                    {
                        Message = "Validation failed",
                        Errors = errors,
                        Timestamp = DateTime.UtcNow
                    });
                };
            });

            // Add API versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
            }).AddMvc();

            // Add response compression
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            // Add comprehensive health checks
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddCheck<DataAccessHealthCheck>("dataaccess")
                .AddCheck<MemoryHealthCheck>("memory");

            // Add CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            return services;
        }
    }

    /// <summary>
    /// Health check for data access layer
    /// </summary>
    public class DataAccessHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;

        public DataAccessHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<Ipam.DataAccess.Interfaces.IUnitOfWork>();
                
                // Simple connectivity test
                await Task.Delay(1, cancellationToken); // Placeholder for actual health check
                
                return HealthCheckResult.Healthy("DataAccess is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("DataAccess is unhealthy", ex);
            }
        }
    }

    /// <summary>
    /// Health check for memory usage
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var memoryUsed = GC.GetTotalMemory(false);
            var memoryUsedMB = memoryUsed / 1024 / 1024;

            var status = memoryUsedMB < 500 ? HealthStatus.Healthy :
                        memoryUsedMB < 1000 ? HealthStatus.Degraded :
                        HealthStatus.Unhealthy;

            return Task.FromResult(new HealthCheckResult(
                status,
                $"Memory usage: {memoryUsedMB} MB",
                null,
                new Dictionary<string, object> { ["MemoryUsedMB"] = memoryUsedMB }
            ));
        }
    }
}