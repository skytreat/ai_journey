using FundRecommendationAPI.Extensions;
using FundRecommendationAPI.Middleware;
using FundRecommendationAPI.Models;
using Microsoft.EntityFrameworkCore;

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Logging.AddFundRecommendationLogging();
    builder.Services.AddFundRecommendationServices(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FundDbContext>();
        db.Database.EnsureCreated();
    }
    
    app.UseRequestLogging();
    
    app.ConfigureRoutes();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error starting server: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    throw;
}
