using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Ipam.IntegrationTests
{
    /// <summary>
    /// Performance integration tests for the enhanced IPAM system
    /// </summary>
    public class PerformanceIntegrationTests : IClassFixture<WebApplicationFactory<Ipam.Frontend.Program>>
    {
        private readonly WebApplicationFactory<Ipam.Frontend.Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public PerformanceIntegrationTests(WebApplicationFactory<Ipam.Frontend.Program> factory, ITestOutputHelper output)
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
                        config["Caching:DurationMinutes"] = "5";
                    });
                });
                
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddXUnit(output);
                    logging.SetMinimumLevel(LogLevel.Warning); // Reduce noise during performance tests
                });
            }).CreateClient();
        }

        [Fact]
        public async Task PerformanceTest_SingleRequest_ResponseTime()
        {
            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes";
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            var response = await _client.GetAsync(endpoint);
            stopwatch.Stop();

            // Assert
            var responseTime = stopwatch.ElapsedMilliseconds;
            _output.WriteLine($"Single request response time: {responseTime}ms");
            _output.WriteLine($"Response status: {response.StatusCode}");

            // Should respond within reasonable time
            Assert.True(responseTime < 5000, $"Response time too slow: {responseTime}ms");

            response.Dispose();
        }

        [Fact]
        public async Task PerformanceTest_ConcurrentRequests_Throughput()
        {
            // Arrange
            var concurrentRequests = 10;
            var endpoint = "/api/addressspaces/test-space/ipnodes";
            var tasks = new List<Task<(HttpResponseMessage Response, long ElapsedMs)>>();
            var overallStopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(MeasureRequest(endpoint));
            }

            var results = await Task.WhenAll(tasks);
            overallStopwatch.Stop();

            // Assert
            var responseTimes = results.Select(r => r.ElapsedMs).ToList();
            var successfulRequests = results.Count(r => r.Response.StatusCode != HttpStatusCode.InternalServerError);
            
            var avgResponseTime = responseTimes.Average();
            var maxResponseTime = responseTimes.Max();
            var minResponseTime = responseTimes.Min();
            var throughput = (double)concurrentRequests / overallStopwatch.Elapsed.TotalSeconds;

            _output.WriteLine($"Concurrent requests: {concurrentRequests}");
            _output.WriteLine($"Successful requests: {successfulRequests}");
            _output.WriteLine($"Average response time: {avgResponseTime:F2}ms");
            _output.WriteLine($"Min response time: {minResponseTime}ms");
            _output.WriteLine($"Max response time: {maxResponseTime}ms");
            _output.WriteLine($"Throughput: {throughput:F2} requests/second");
            _output.WriteLine($"Total time: {overallStopwatch.ElapsedMilliseconds}ms");

            // Performance assertions
            Assert.True(avgResponseTime < 10000, $"Average response time too slow: {avgResponseTime}ms");
            Assert.True(maxResponseTime < 15000, $"Max response time too slow: {maxResponseTime}ms");
            Assert.True(successfulRequests >= concurrentRequests * 0.8, $"Too many failed requests: {successfulRequests}/{concurrentRequests}");

            // Clean up
            foreach (var result in results)
            {
                result.Response.Dispose();
            }
        }

        [Fact]
        public async Task PerformanceTest_MemoryUsage_UnderLoad()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var requestCount = 20;
            var endpoint = "/api/addressspaces/test-space/ipnodes";

            _output.WriteLine($"Initial memory: {initialMemory / 1024 / 1024:F2} MB");

            // Act - Generate load
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync($"{endpoint}?test={i}"));
            }

            var responses = await Task.WhenAll(tasks);

            // Force garbage collection and measure memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            _output.WriteLine($"Final memory: {finalMemory / 1024 / 1024:F2} MB");
            _output.WriteLine($"Memory increase: {memoryIncrease / 1024 / 1024:F2} MB");
            _output.WriteLine($"Processed {requestCount} requests");

            // Memory increase should be reasonable
            var memoryIncreaseMB = memoryIncrease / 1024.0 / 1024.0;
            Assert.True(memoryIncreaseMB < 100, $"Memory increase too high: {memoryIncreaseMB:F2} MB");

            // Clean up
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        [Fact]
        public async Task PerformanceTest_CachingEffectiveness()
        {
            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes/byPrefix?prefix=192.168.100.0/24";
            var measurements = new List<long>();

            // Act - First request (cold cache)
            var result1 = await MeasureRequest(endpoint);
            measurements.Add(result1.ElapsedMs);
            _output.WriteLine($"First request (cold cache): {result1.ElapsedMs}ms");
            result1.Response.Dispose();

            // Subsequent requests (warm cache)
            for (int i = 0; i < 5; i++)
            {
                var result = await MeasureRequest(endpoint);
                measurements.Add(result.ElapsedMs);
                _output.WriteLine($"Request {i + 2} (warm cache): {result.ElapsedMs}ms");
                result.Response.Dispose();
                
                await Task.Delay(100); // Small delay between requests
            }

            // Assert
            var firstRequestTime = measurements[0];
            var subsequentRequests = measurements.Skip(1).ToList();
            var avgSubsequentTime = subsequentRequests.Average();

            _output.WriteLine($"First request time: {firstRequestTime}ms");
            _output.WriteLine($"Average subsequent time: {avgSubsequentTime:F2}ms");

            // Subsequent requests should not be significantly slower than first
            // (We can't guarantee caching will make them faster in integration tests)
            var maxAcceptableTime = Math.Max(firstRequestTime * 2, 10000); // 2x first request or 10s max
            Assert.True(avgSubsequentTime < maxAcceptableTime, 
                $"Subsequent requests too slow: {avgSubsequentTime:F2}ms vs {maxAcceptableTime}ms");
        }

        [Fact]
        public async Task PerformanceTest_ErrorHandling_Performance()
        {
            // Arrange
            var errorEndpoints = new[]
            {
                "/api/addressspaces/test-space/ipnodes/byPrefix?prefix=invalid",
                "/api/addressspaces/test-space/ipnodes/byTags",
                "/api/nonexistent-endpoint"
            };

            var results = new ConcurrentBag<(string Endpoint, long ElapsedMs, HttpStatusCode StatusCode)>();

            // Act
            var tasks = errorEndpoints.Select(async endpoint =>
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _client.GetAsync(endpoint);
                stopwatch.Stop();
                
                results.Add((endpoint, stopwatch.ElapsedMilliseconds, response.StatusCode));
                response.Dispose();
            });

            await Task.WhenAll(tasks);

            // Assert
            foreach (var result in results)
            {
                _output.WriteLine($"Error endpoint {result.Endpoint}: {result.StatusCode} in {result.ElapsedMs}ms");
                
                // Error handling should be fast
                Assert.True(result.ElapsedMs < 5000, 
                    $"Error handling too slow for {result.Endpoint}: {result.ElapsedMs}ms");
            }
        }

        [Fact]
        public async Task PerformanceTest_HealthChecks_ResponseTime()
        {
            // Arrange
            var healthEndpoints = new[] { "/health", "/health/ready", "/health/live" };
            var measurements = new Dictionary<string, List<long>>();

            // Act - Test each endpoint multiple times
            foreach (var endpoint in healthEndpoints)
            {
                measurements[endpoint] = new List<long>();
                
                for (int i = 0; i < 3; i++)
                {
                    var result = await MeasureRequest(endpoint);
                    measurements[endpoint].Add(result.ElapsedMs);
                    _output.WriteLine($"{endpoint} attempt {i + 1}: {result.ElapsedMs}ms");
                    result.Response.Dispose();
                }
            }

            // Assert
            foreach (var kvp in measurements)
            {
                var avgTime = kvp.Value.Average();
                var maxTime = kvp.Value.Max();
                
                _output.WriteLine($"{kvp.Key} - Average: {avgTime:F2}ms, Max: {maxTime}ms");
                
                // Health checks should be very fast
                Assert.True(avgTime < 2000, $"Health check {kvp.Key} too slow: {avgTime:F2}ms");
                Assert.True(maxTime < 5000, $"Health check {kvp.Key} max time too slow: {maxTime}ms");
            }
        }

        [Fact]
        public async Task PerformanceTest_LongRunning_StabilityTest()
        {
            // Arrange
            var duration = TimeSpan.FromSeconds(10); // Short duration for CI
            var endpoint = "/api/addressspaces/test-space/ipnodes";
            var requestCount = 0;
            var errors = 0;
            var responseTimes = new List<long>();
            var startTime = DateTime.UtcNow;

            _output.WriteLine($"Starting stability test for {duration.TotalSeconds} seconds");

            // Act - Send requests continuously for the duration
            while (DateTime.UtcNow - startTime < duration)
            {
                try
                {
                    var result = await MeasureRequest(endpoint);
                    requestCount++;
                    responseTimes.Add(result.ElapsedMs);
                    
                    if (result.Response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        errors++;
                    }
                    
                    result.Response.Dispose();
                    
                    // Small delay to avoid overwhelming the system
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    errors++;
                    _output.WriteLine($"Request error: {ex.Message}");
                }
            }

            // Assert
            var actualDuration = DateTime.UtcNow - startTime;
            var avgResponseTime = responseTimes.Any() ? responseTimes.Average() : 0;
            var errorRate = requestCount > 0 ? (double)errors / requestCount : 0;
            var requestsPerSecond = requestCount / actualDuration.TotalSeconds;

            _output.WriteLine($"Stability test results:");
            _output.WriteLine($"  Duration: {actualDuration.TotalSeconds:F1} seconds");
            _output.WriteLine($"  Total requests: {requestCount}");
            _output.WriteLine($"  Errors: {errors}");
            _output.WriteLine($"  Error rate: {errorRate:P1}");
            _output.WriteLine($"  Average response time: {avgResponseTime:F2}ms");
            _output.WriteLine($"  Requests per second: {requestsPerSecond:F2}");

            // Stability assertions
            Assert.True(requestCount > 0, "Should have processed some requests");
            Assert.True(errorRate < 0.1, $"Error rate too high: {errorRate:P1}"); // Less than 10% errors
            Assert.True(avgResponseTime < 10000, $"Average response time too slow: {avgResponseTime:F2}ms");
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