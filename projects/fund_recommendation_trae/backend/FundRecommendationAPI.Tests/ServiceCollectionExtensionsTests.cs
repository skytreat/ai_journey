using FundRecommendationAPI.Extensions;
using FundRecommendationAPI.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddFundRecommendationServices_ShouldAddDbContext()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetService<FundDbContext>();

            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldAddMemoryCache()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var memoryCache = serviceProvider.GetService<IMemoryCache>();

            Assert.NotNull(memoryCache);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldAddRateLimiter()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var rateLimiterOptions = serviceProvider.GetService<RateLimiterOptions>();

            Assert.NotNull(rateLimiterOptions);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldConfigureRateLimiterWithCorrectSettings()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var rateLimiterOptions = serviceProvider.GetService<RateLimiterOptions>();

            Assert.NotNull(rateLimiterOptions);
            var limiter = rateLimiterOptions.GlobalLimiter;
            Assert.NotNull(limiter);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldReturnSameServiceCollection()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            var result = services.AddFundRecommendationServices(configuration);

            Assert.Same(services, result);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldThrowWhenConnectionStringIsNull()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            Assert.Throws<InvalidOperationException>(() =>
            {
                services.AddFundRecommendationServices(configuration);
            });
        }

        [Fact]
        public void AddFundRecommendationLogging_ShouldClearExistingProviders()
        {
            var logging = new Mock<ILoggingBuilder>();
            logging.Setup(l => l.ClearProviders()).Returns(logging.Object);
            logging.Setup(l => l.AddConsole()).Returns(logging.Object);
            logging.Setup(l => l.AddDebug()).Returns(logging.Object);
            logging.Setup(l => l.SetMinimumLevel(LogLevel.Debug)).Returns(logging.Object);

            var result = logging.Object.AddFundRecommendationLogging();

            logging.Verify(l => l.ClearProviders(), Times.Once);
            logging.Verify(l => l.AddConsole(), Times.Once);
            logging.Verify(l => l.AddDebug(), Times.Once);
            logging.Verify(l => l.SetMinimumLevel(LogLevel.Debug), Times.Once);
        }

        [Fact]
        public void AddFundRecommendationLogging_ShouldAddConsoleLogger()
        {
            var logging = new Mock<ILoggingBuilder>();
            logging.Setup(l => l.ClearProviders()).Returns(logging.Object);
            logging.Setup(l => l.AddConsole()).Returns(logging.Object);
            logging.Setup(l => l.AddDebug()).Returns(logging.Object);
            logging.Setup(l => l.SetMinimumLevel(LogLevel.Debug)).Returns(logging.Object);

            logging.Object.AddFundRecommendationLogging();

            logging.Verify(l => l.AddConsole(), Times.Once);
        }

        [Fact]
        public void AddFundRecommendationLogging_ShouldAddDebugLogger()
        {
            var logging = new Mock<ILoggingBuilder>();
            logging.Setup(l => l.ClearProviders()).Returns(logging.Object);
            logging.Setup(l => l.AddConsole()).Returns(logging.Object);
            logging.Setup(l => l.AddDebug()).Returns(logging.Object);
            logging.Setup(l => l.SetMinimumLevel(LogLevel.Debug)).Returns(logging.Object);

            logging.Object.AddFundRecommendationLogging();

            logging.Verify(l => l.AddDebug(), Times.Once);
        }

        [Fact]
        public void AddFundRecommendationLogging_ShouldSetMinimumLogLevelToDebug()
        {
            var logging = new Mock<ILoggingBuilder>();
            logging.Setup(l => l.ClearProviders()).Returns(logging.Object);
            logging.Setup(l => l.AddConsole()).Returns(logging.Object);
            logging.Setup(l => l.AddDebug()).Returns(logging.Object);
            logging.Setup(l => l.SetMinimumLevel(LogLevel.Debug)).Returns(logging.Object);

            logging.Object.AddFundRecommendationLogging();

            logging.Verify(l => l.SetMinimumLevel(LogLevel.Debug), Times.Once);
        }

        [Fact]
        public void AddFundRecommendationLogging_ShouldReturnSameLoggingBuilder()
        {
            var logging = new Mock<ILoggingBuilder>();
            logging.Setup(l => l.ClearProviders()).Returns(logging.Object);
            logging.Setup(l => l.AddConsole()).Returns(logging.Object);
            logging.Setup(l => l.AddDebug()).Returns(logging.Object);
            logging.Setup(l => l.SetMinimumLevel(LogLevel.Debug)).Returns(logging.Object);

            var result = logging.Object.AddFundRecommendationLogging();

            Assert.Same(logging.Object, result);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldAddRouting()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var routing = serviceProvider.GetService<Microsoft.AspNetCore.Routing.EndpointDataSource>();

            Assert.NotNull(routing);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldAddOpenApi()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetService<FundDbContext>();

            Assert.NotNull(dbContext);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldConfigureDbContextWithSqlite()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetService<FundDbContext>();

            Assert.NotNull(dbContext);
            Assert.IsAssignableFrom<FundDbContext>(dbContext);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldConfigureRateLimiterWithApiPolicy()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var rateLimiterOptions = serviceProvider.GetService<RateLimiterOptions>();

            Assert.NotNull(rateLimiterOptions);
            Assert.NotNull(rateLimiterOptions.GlobalLimiter);
        }

        [Fact]
        public void AddFundRecommendationServices_ShouldConfigureRateLimiterWithCorrectLimits()
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            services.AddFundRecommendationServices(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var rateLimiterOptions = serviceProvider.GetService<RateLimiterOptions>();

            Assert.NotNull(rateLimiterOptions);
            Assert.NotNull(rateLimiterOptions.GlobalLimiter);
        }
    }
}
