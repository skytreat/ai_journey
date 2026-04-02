using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using System;

namespace FundRecommendationAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFundRecommendationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpenApi();
            services.AddSwaggerGen();
            services.AddRouting();
            services.AddDbContext<FundDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
            services.AddMemoryCache();
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("api", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 10;
                });
            });

            services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
            services.AddScoped<IHealthCheckService, HealthCheckService>();

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<ISystemService, SystemService>();
            services.AddScoped<IFundAnalysisService, FundAnalysisService>();
            services.AddScoped<IFundDataService, FundDataService>();
            // services.AddHostedService<FundDataCollectorService>();

            return services;
        }

        public static ILoggingBuilder AddFundRecommendationLogging(this ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Debug);

            return logging;
        }
    }
}
