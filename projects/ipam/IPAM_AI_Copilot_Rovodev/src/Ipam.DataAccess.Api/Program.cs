using Ipam.DataAccess;
using Ipam.DataAccess.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ipam.DataAccess.Api
{
    /// <summary>
    /// Data Access Microservice entry point
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

            // Add Authorization
            builder.Services.AddAuthorization();

            // Add controllers and API services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "IPAM Data Access API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new()
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
            });

            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(Program));

            // Add OpenTelemetry
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Ipam.DataAccess.Api"))
                        .AddAspNetCoreInstrumentation()
                        .AddSource("Ipam.DataAccess");
                });

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

            // Add health checks
            builder.Services.AddHealthChecks();

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
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}