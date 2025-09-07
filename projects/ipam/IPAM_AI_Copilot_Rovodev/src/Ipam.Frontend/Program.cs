using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Ipam.DataAccess;
using Ipam.DataAccess.Configuration;

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
            var builder = WebApplication.CreateBuilder(args);

            // Add IPAM DataAccess services
            builder.Services.AddIpamDataAccess(options =>
            {
                options.ConnectionString = builder.Configuration.GetConnectionString("AzureTableStorage") ?? "UseDevelopmentStorage=true";
                options.EnableCaching = true;
                options.CacheDuration = TimeSpan.FromMinutes(5);
            });

            // Add controllers and API services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(Program));

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}