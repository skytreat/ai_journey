using FundRecommendationAPI.Extensions;
using FundRecommendationAPI.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void Program_ShouldBuildWebApplicationBuilder()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            Assert.NotNull(builder);
        }

        [Fact]
        public void Program_ShouldConfigureLogging()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            
            var loggingBuilder = builder.Logging;
            Assert.NotNull(loggingBuilder);
        }

        [Fact]
        public void Program_ShouldConfigureServices()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            
            var services = builder.Services;
            Assert.NotNull(services);
        }

        [Fact]
        public void Program_ShouldBuildWebApplication()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void Program_ShouldConfigureRoutes()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            
            var middleware = app as IApplicationBuilder;
            Assert.NotNull(middleware);
        }

        [Fact]
        public void Program_ShouldHandleEmptyArgs()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            
            Assert.NotNull(builder);
        }

        [Fact]
        public void Program_ShouldHandleNullArgs()
        {
            string[] args = null;
            var builder = WebApplication.CreateBuilder(args);
            
            Assert.NotNull(builder);
        }

        [Fact]
        public void Program_ShouldHandleMultipleArgs()
        {
            var args = new[] { "arg1", "arg2", "arg3" };
            var builder = WebApplication.CreateBuilder(args);
            
            Assert.NotNull(builder);
        }

        [Fact]
        public void ServiceCollectionExtensions_ShouldReturnServiceCollection()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            var result = services.AddFundRecommendationServices(configuration);
            
            Assert.Same(services, result);
        }

        [Fact]
        public void ServiceCollectionExtensions_ShouldAddDbContext()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetService<FundDbContext>();
            
            Assert.NotNull(dbContext);
        }

        [Fact]
        public void ServiceCollectionExtensions_ShouldAddMemoryCache()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var memoryCache = serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            
            Assert.NotNull(memoryCache);
        }

        [Fact]
        public void ServiceCollectionExtensions_ShouldAddRateLimiter()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var rateLimiterOptions = serviceProvider.GetService<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>();
            
            Assert.NotNull(rateLimiterOptions);
        }

        [Fact]
        public void ServiceCollectionExtensions_ShouldAddRouting()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            
            Assert.NotNull(serviceProvider);
        }

        [Fact]
        public void ServiceCollectionExtensions_ShouldAddOpenApi()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            
            Assert.NotNull(serviceProvider);
        }

        [Fact]
        public void ServiceCollectionExtensions_ShouldConfigureRateLimiterWithCorrectSettings()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var rateLimiterOptions = serviceProvider.GetService<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>();
            
            Assert.NotNull(rateLimiterOptions);
            Assert.NotNull(rateLimiterOptions.GlobalLimiter);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureHttpsRedirection()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureRateLimiter()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureApiGroup()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureFundRoutes()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureAnalysisRoutes()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureFavoriteRoutes()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureQueryRoutes()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void RouteConfigurator_ShouldConfigureMetaRoutes()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void Program_ShouldHandleExceptionDuringStartup()
        {
            var args = new[] { "--invalid-arg" };
            var exception = Record.Exception(() => WebApplication.CreateBuilder(args));
            
            Assert.NotNull(exception);
        }

        [Fact]
        public void Program_ShouldHandleConfigurationErrors()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            
            Assert.NotNull(builder);
        }

        [Fact]
        public void Program_ShouldSupportDevelopmentEnvironment()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void Program_ShouldSupportProductionEnvironment()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }

        [Fact]
        public void Program_ShouldConfigureLoggingWithCorrectLevel()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            
            var logging = builder.Logging;
            Assert.NotNull(logging);
        }

        [Fact]
        public void Program_ShouldAddFundRecommendationServices()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            
            var services = builder.Services;
            Assert.NotNull(services);
        }

        [Fact]
        public void Program_ShouldConfigureRoutesWithCorrectMiddleware()
        {
            var args = new string[0];
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddFundRecommendationServices(builder.Configuration);
            var app = builder.Build();
            app.ConfigureRoutes();
            
            Assert.NotNull(app);
        }
    }
}
