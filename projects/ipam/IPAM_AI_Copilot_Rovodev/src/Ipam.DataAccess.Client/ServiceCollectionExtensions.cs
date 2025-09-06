using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Client.Configuration;

namespace Ipam.DataAccess.Client
{
    /// <summary>
    /// Service collection extensions for Data Access API client
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Data Access API client services
        /// </summary>
        public static IServiceCollection AddDataAccessApiClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<DataAccessApiOptions>(
                configuration.GetSection(DataAccessApiOptions.SectionName));

            services.AddHttpClient<IDataAccessApiClient, DataAccessApiClient>();

            return services;
        }

        /// <summary>
        /// Add Data Access API client services with custom configuration
        /// </summary>
        public static IServiceCollection AddDataAccessApiClient(
            this IServiceCollection services,
            Action<DataAccessApiOptions> configure)
        {
            services.Configure<DataAccessApiOptions>(configure);
            services.AddHttpClient<IDataAccessApiClient, DataAccessApiClient>();

            return services;
        }
    }
}