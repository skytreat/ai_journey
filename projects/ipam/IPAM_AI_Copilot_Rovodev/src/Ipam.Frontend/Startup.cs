using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ipam.DataAccess;
using Ipam.DataAccess.Services;
using Ipam.ServiceContract.Interfaces;

namespace Ipam.Frontend
{
    /// <summary>
    /// Application startup configuration
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddIpamDataAccess(options =>
            {
                options.ConnectionString = Configuration.GetConnectionString("AzureTableStorage") ?? "UseDevelopmentStorage=true";
                options.EnableCaching = true;
                options.CacheDuration = TimeSpan.FromMinutes(5);
            });

            // Register individual service interfaces for dependency injection
            services.AddScoped<IAddressSpaceService, AddressSpaceService>();
            services.AddScoped<IIpAllocationService, IpAllocationServiceImpl>();
            services.AddScoped<ITagService, TagServiceImpl>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}