using Ipam.DataAccess.Configuration;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

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
            services.AddScoped<IIpNodeRepository, IpNodeRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .AddSource("Ipam.DataAccess")
                    .AddAzureTableClientInstrumentation());

            services.AddHostedService<DatabaseInitializationService>();

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<DataAccessOptions>>().Value;

            if (options.EnableCaching)
            {
                services.Decorate<IIpNodeRepository, CachingIpNodeRepository>();
                services.AddMemoryCache();
            }

            return services;
        }
    }
}
