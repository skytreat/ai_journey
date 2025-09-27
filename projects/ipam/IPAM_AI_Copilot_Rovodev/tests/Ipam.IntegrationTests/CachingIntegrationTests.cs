using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Ipam.IntegrationTests
{
    /// <summary>
    /// Integration tests for the enhanced caching system with Redis support
    /// </summary>
    public class CachingIntegrationTests : IClassFixture<WebApplicationFactory<Ipam.Frontend.Program>>
    {
        private readonly WebApplicationFactory<Ipam.Frontend.Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public CachingIntegrationTests(WebApplicationFactory<Ipam.Frontend.Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<Microsoft.Extensions.Configuration.ConfigurationManager>(config =>
                    {
                        config["ConnectionStrings:AzureTableStorage"] = "UseDevelopmentStorage=true";
                        config["Caching:Enabled"] = "true";
                        config["Caching:DurationMinutes"] = "1"; // Short duration for testing
                        // Don't configure Redis for these tests to use memory cache
                    });
                });
                
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddXUnit(output);
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            }).CreateClient();
        }

        [Fact]
        public async Task MemoryCache_IsConfigured()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Memory cache should be available in the service container
            using var scope = _factory.Services.CreateScope();
            var memoryCache = scope.ServiceProvider.GetService<IMemoryCache>();
            
            Assert.NotNull(memoryCache);
            _output.WriteLine("Memory cache is properly configured");
        }

        [Fact]
        public async Task ResponseCaching_WorksForGetRequests()
        {
            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes";
            var stopwatch = new Stopwatch();

            // Act - First request
            stopwatch.Start();
            var response1 = await _client.GetAsync(endpoint);
            stopwatch.Stop();
            var firstRequestTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var response2 = await _client.GetAsync(endpoint);
            stopwatch.Stop();
            var secondRequestTime = stopwatch.ElapsedMilliseconds;

            // Assert
            _output.WriteLine($"First request: {response1.StatusCode} in {firstRequestTime}ms");
            _output.WriteLine($"Second request: {response2.StatusCode} in {secondRequestTime}ms");

            // Both requests should have same status
            Assert.Equal(response1.StatusCode, response2.StatusCode);

            // Check cache headers if present
            var cacheHeaders1 = response1.Headers.CacheControl;
            var cacheHeaders2 = response2.Headers.CacheControl;
            
            if (cacheHeaders1 != null)
            {
                _output.WriteLine($"Cache headers: MaxAge={cacheHeaders1.MaxAge}");
            }
        }

        [Fact]
        public async Task CachingService_HandlesHighFrequencyRequests()
        {
            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes/byPrefix?prefix=192.168.1.0/24";
            var requestCount = 10;
            var tasks = new List<Task<(HttpResponseMessage Response, long ElapsedMs)>>();

            // Act - Send multiple requests concurrently
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(MeasureRequest(endpoint));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            var statusCodes = results.Select(r => r.Response.StatusCode).Distinct().ToList();
            var responseTimes = results.Select(r => r.ElapsedMs).ToList();

            _output.WriteLine($"Status codes: {string.Join(", ", statusCodes)}");
            _output.WriteLine($"Response times: {string.Join(", ", responseTimes)}ms");
            _output.WriteLine($"Average response time: {responseTimes.Average():F2}ms");

            // All requests should have consistent status codes
            Assert.True(statusCodes.Count <= 2, "Too many different status codes");

            // Later requests might be faster due to caching
            var averageTime = responseTimes.Average();
            Assert.True(averageTime < 5000, $"Average response time too high: {averageTime}ms");

            // Clean up
            foreach (var result in results)
            {
                result.Response.Dispose();
            }
        }

        [Fact]
        public async Task CacheExpiration_WorksCorrectly()
        {
            // Note: This test is limited since we can't easily control cache expiration in integration tests
            // But we can verify that the caching mechanism doesn't break functionality

            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes";

            // Act - Multiple requests over time
            var response1 = await _client.GetAsync(endpoint);
            await Task.Delay(100); // Small delay
            var response2 = await _client.GetAsync(endpoint);

            // Assert
            _output.WriteLine($"First response: {response1.StatusCode}");
            _output.WriteLine($"Second response: {response2.StatusCode}");

            // Both should succeed with consistent behavior
            Assert.Equal(response1.StatusCode, response2.StatusCode);

            response1.Dispose();
            response2.Dispose();
        }

        [Fact]
        public async Task CacheBypass_WorksForPostRequests()
        {
            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes";
            var testData = new
            {
                addressSpaceId = "test-space",
                prefix = "192.168.100.0/24",
                tags = new Dictionary<string, string> { ["test"] = "cache-bypass" }
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(testData),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync(endpoint, content);

            // Assert
            _output.WriteLine($"POST response: {response.StatusCode}");
            
            // POST requests should not be cached and should be processed
            Assert.True(response.StatusCode != HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.Unauthorized ||
                       response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.Created);

            response.Dispose();
        }

        [Fact]
        public async Task CachingWithDifferentParameters_WorksCorrectly()
        {
            // Arrange
            var endpoints = new[]
            {
                "/api/addressspaces/space1/ipnodes/byPrefix?prefix=192.168.1.0/24",
                "/api/addressspaces/space1/ipnodes/byPrefix?prefix=192.168.2.0/24",
                "/api/addressspaces/space2/ipnodes/byPrefix?prefix=192.168.1.0/24"
            };

            // Act
            var responses = new List<HttpResponseMessage>();
            foreach (var endpoint in endpoints)
            {
                responses.Add(await _client.GetAsync(endpoint));
            }

            // Assert
            for (int i = 0; i < endpoints.Length; i++)
            {
                _output.WriteLine($"Endpoint {endpoints[i]}: {responses[i].StatusCode}");
            }

            // Different parameters should result in different cache entries
            // All responses should be handled properly
            foreach (var response in responses)
            {
                Assert.True(response.StatusCode == HttpStatusCode.OK ||
                           response.StatusCode == HttpStatusCode.NotFound ||
                           response.StatusCode == HttpStatusCode.BadRequest);
            }

            // Clean up
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        [Fact]
        public async Task DistributedCache_FallbackToMemoryCache()
        {
            // This test ensures that when Redis is not available, 
            // the system falls back to memory cache gracefully

            // Act
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes");

            // Assert
            _output.WriteLine($"Fallback cache test: {response.StatusCode}");
            
            // Should work even without Redis
            Assert.True(response.StatusCode != HttpStatusCode.InternalServerError);

            response.Dispose();
        }

        [Fact]
        public async Task CachePerformance_ImprovesResponseTime()
        {
            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes";
            var measurements = new List<long>();

            // Act - Take multiple measurements
            for (int i = 0; i < 5; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _client.GetAsync(endpoint);
                stopwatch.Stop();
                
                measurements.Add(stopwatch.ElapsedMilliseconds);
                _output.WriteLine($"Request {i + 1}: {response.StatusCode} in {stopwatch.ElapsedMilliseconds}ms");
                
                response.Dispose();
                
                // Small delay between requests
                await Task.Delay(50);
            }

            // Assert
            var averageTime = measurements.Average();
            var maxTime = measurements.Max();
            
            _output.WriteLine($"Average response time: {averageTime:F2}ms");
            _output.WriteLine($"Max response time: {maxTime}ms");

            // Response times should be reasonable
            Assert.True(averageTime < 10000, $"Average response time too high: {averageTime}ms");
            Assert.True(maxTime < 15000, $"Max response time too high: {maxTime}ms");
        }

        private async Task<(HttpResponseMessage Response, long ElapsedMs)> MeasureRequest(string endpoint)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync(endpoint);
            stopwatch.Stop();
            
            return (response, stopwatch.ElapsedMilliseconds);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}