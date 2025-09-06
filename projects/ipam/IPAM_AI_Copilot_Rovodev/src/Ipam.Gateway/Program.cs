using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

namespace Ipam.Gateway
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
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "DefaultSecretKeyForDevelopment123456789");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"] ?? "IpamSystem",
                        ValidAudience = jwtSettings["Audience"] ?? "IpamUsers",
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });

            // Add Authorization with RBAC policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SystemAdmin", policy => 
                    policy.RequireClaim("role", "SystemAdmin"));
                options.AddPolicy("AddressSpaceAdmin", policy => 
                    policy.RequireClaim("role", "AddressSpaceAdmin", "SystemAdmin"));
                options.AddPolicy("AddressSpaceOperator", policy => 
                    policy.RequireClaim("role", "AddressSpaceOperator", "AddressSpaceAdmin", "SystemAdmin"));
                options.AddPolicy("AddressSpaceViewer", policy => 
                    policy.RequireClaim("role", "AddressSpaceViewer", "AddressSpaceOperator", "AddressSpaceAdmin", "SystemAdmin"));
            });

            // Add Ocelot for API Gateway functionality
            builder.Services.AddOcelot();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Add controllers for health checks and authentication
            builder.Services.AddControllers();

            // Add Swagger for API documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Use Ocelot middleware
            await app.UseOcelot();

            app.Run();
        }
    }
}