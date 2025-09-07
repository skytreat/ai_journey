using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ipam.WebPortal
{
    /// <summary>
    /// Web Portal entry point
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

            // Add services for MVC
            builder.Services.AddControllersWithViews();

            // Add HttpClient service
            builder.Services.AddHttpClient();

            var app = builder.Build();

            // Serve static files (e.g., Bootstrap CSS/JS)
            app.UseStaticFiles();

            app.UseRouting();

            // Default route mapping
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Health Check Endpoint
            app.MapGet("/health", () => "Web Portal healthy");

            app.Run();
        }
    }
}