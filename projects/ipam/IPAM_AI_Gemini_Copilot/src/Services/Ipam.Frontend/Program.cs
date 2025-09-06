using System.Text;
using Ipam.DataAccess;
using Ipam.Frontend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

namespace Ipam.Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure OpenTelemetry
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter());

            // Configure DataAccess Layer
            var connectionString = builder.Configuration.GetConnectionString("AzureTableStorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("AzureTableStorage connection string is missing.");
            }
            builder.Services.AddDataAccessServices(connectionString);

            // Configure Business Logic Services
            builder.Services.AddScoped<IIpamService, IpamService>();

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is missing in configuration.");
            }
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
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });

            // Configure Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SystemAdmin", policy => policy.RequireRole("SystemAdmin"));
                options.AddPolicy("AddressSpaceAdmin", policy => policy.RequireRole("SystemAdmin", "AddressSpaceAdmin"));
                options.AddPolicy("AddressSpaceViewer", policy => policy.RequireRole("SystemAdmin", "AddressSpaceAdmin", "AddressSpaceViewer"));
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // Show detailed exceptions in development
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
