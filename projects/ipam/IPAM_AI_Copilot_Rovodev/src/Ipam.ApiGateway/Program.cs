using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Ipam.ApiGateway
{
    /// <summary>
    /// API Gateway entry point with authentication, authorization, and rate limiting
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

            // Add JWT authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });

            // Add authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
            });

            // Add rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("api", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 100;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 50;
                });
            });

            var app = builder.Build();

            // Use authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Use rate limiting
            app.UseRateLimiter();

            // Health Check Endpoint
            app.MapGet("/health", () => "API Gateway healthy");

            // Map API routes
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    // Forward requests to Frontend service
                    var frontendUrl = builder.Configuration["FrontendServiceUrl"];
                    var path = context.Request.Path;
                    var queryString = context.Request.QueryString;
                    var targetUrl = $"{frontendUrl}{path}{queryString}";

                    // Forward the request
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(targetUrl);
                    var content = await response.Content.ReadAsStringAsync();
                    
                    context.Response.StatusCode = (int)response.StatusCode;
                    await context.Response.WriteAsync(content);
                });
            });

            app.Run();
        }
    }
}
