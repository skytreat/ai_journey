using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Ipam.DataAccess;
using Ipam.DataAccess.Configuration;
using Ipam.Frontend.Extensions;
using Serilog;
using Serilog.Events;

namespace Ipam.Frontend
{
    /// <summary>
    /// Frontend service entry point
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "IPAM.Frontend")
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File("logs/ipam-frontend-.log", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Starting IPAM Frontend service");
                
                var builder = WebApplication.CreateBuilder(args);

                // Use Serilog
                builder.Host.UseSerilog();

                // Add IPAM DataAccess services with enhanced configuration
                builder.Services.AddIpamDataAccess(options =>
                {
                    options.ConnectionString = builder.Configuration.GetConnectionString("AzureTableStorage") ?? "UseDevelopmentStorage=true";
                    options.EnableCaching = builder.Configuration.GetValue<bool>("Caching:Enabled", true);
                    options.CacheDuration = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Caching:DurationMinutes", 5));
                    options.MaxRetryAttempts = builder.Configuration.GetValue<int>("DataAccess:MaxRetryAttempts", 3);
                });

                // Add distributed caching if Redis is configured
                var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
                if (!string.IsNullOrEmpty(redisConnectionString))
                {
                    builder.Services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = redisConnectionString;
                        options.InstanceName = "IPAM";
                    });
                    Log.Information("Redis distributed caching enabled");
                }

                // Add frontend services
                builder.Services.AddFrontendServices();

                // Add API documentation
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new() { 
                        Title = "IPAM API", 
                        Version = "v1",
                        Description = "IP Address Management System API"
                    });
                    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Ipam.Frontend.xml"), true);
                });

                var app = builder.Build();

                // Configure the HTTP request pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IPAM API V1");
                        c.RoutePrefix = string.Empty; // Serve at root
                    });
                }

                // Add security headers
                app.Use(async (context, next) =>
                {
                    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    context.Response.Headers.Add("X-Frame-Options", "DENY");
                    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                    await next();
                });

                // Add global error handling middleware
                app.UseMiddleware<Ipam.Frontend.Middleware.GlobalErrorHandlingMiddleware>();
                
                app.UseHttpsRedirection();
                app.UseCors();
                app.UseResponseCompression();
                app.UseAuthentication();
                app.UseAuthorization();

                // Map health checks
                app.MapHealthChecks("/health");
                app.MapHealthChecks("/health/ready");
                app.MapHealthChecks("/health/live");

                app.MapControllers();

                Log.Information("IPAM Frontend service started successfully");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "IPAM Frontend service failed to start");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}