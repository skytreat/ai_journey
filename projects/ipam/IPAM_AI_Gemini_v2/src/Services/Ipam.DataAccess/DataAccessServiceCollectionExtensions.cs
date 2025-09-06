
using Azure.Data.Tables;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ipam.DataAccess
{
    public static class DataAccessServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccessServices(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton(new TableServiceClient(connectionString));
            services.AddScoped<IAddressSpaceRepository, AddressSpaceRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IIpAddressRepository, IpAddressRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            return services;
        }
    }
}
