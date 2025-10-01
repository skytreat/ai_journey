using Ipam.DataAccess.Configuration;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Mapping;
using Ipam.DataAccess.Repositories;
using Ipam.DataAccess.Repositories.Decorators;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Telemetry;
using Ipam.ServiceContract.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using System;

namespace Ipam.DataAccess
{
    /// <summary>
    /// Extension methods for configuring data access services
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public static class DataAccessServiceCollectionExtensions
    {
        public static IServiceCollection AddIpamDataAccess(
            this IServiceCollection services,
            Action<DataAccessOptions> configure)
        {
            services.Configure<DataAccessOptions>(configure);

            services.AddScoped<IAddressSpaceRepository, AddressSpaceRepository>();
            services.AddScoped<IIpAllocationRepository, IpAllocationRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Register domain services (used by service implementations)
            services.AddScoped<TagInheritanceService>();
            services.AddScoped<IpTreeService>();
            services.AddScoped<ConcurrentIpTreeService>();
            services.AddScoped<OptimizedIpTreeTraversalService>();
            services.AddScoped<TreeOperationOptimizer>();
            services.AddScoped<AuditService>();
            services.AddSingleton<PerformanceMonitoringService>();
            
            // Register service implementations with interfaces
            services.AddScoped<IAddressSpaceService, AddressSpaceService>();
            services.AddScoped<IIpAllocationService, IpAllocationServiceImpl>();
            services.AddScoped<ITagService, TagServiceImpl>();

            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .AddSource("Ipam.DataAccess")
                    .AddAzureTableClientInstrumentation());

            services.AddHostedService<DatabaseInitializationService>();

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<DataAccessOptions>>().Value;

            // Always add memory cache
            services.AddMemoryCache();
            
            if (options.EnableCaching)
            {
                // Replace the existing repository registration with caching decorator
                services.AddScoped<IIpAllocationRepository>(provider =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var baseRepository = new IpAllocationRepository(configuration);
                    var cache = provider.GetRequiredService<IMemoryCache>();
                    var cachingOptions = provider.GetRequiredService<IOptions<DataAccessOptions>>();
                    return new CachingIpAllocationRepository(baseRepository, cache, cachingOptions);
                });
            }

            // Add AutoMapper
            services.AddAutoMapper(typeof(EntityDtoMappingProfile));

            return services;
        }
    }
}