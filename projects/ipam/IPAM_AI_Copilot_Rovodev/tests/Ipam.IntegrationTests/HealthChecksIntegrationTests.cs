using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Ipam.IntegrationTests
{
    /// <summary>
    /// Integration tests for the enhanced health checks system
    /// </summary>
    public class HealthChecksIntegrationTests : IClassFixture<WebApplicationFactory<Ipam.Frontend.Program>>
    {
        private readonly WebApplicationFactory<Ipam.Frontend.Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public HealthChecksIntegrationTests(WebApplicationFactory<Ipam.Frontend.Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override configuration for testing
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
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            }).CreateClient();
        }

        [Fact]
        public async Task HealthCheck_BasicEndpoint_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Health check response: {content}");
            
            Assert.Contains("Healthy", content);
        }

        [Fact]
        public async Task HealthCheck_ReadyEndpoint_ReturnsStatus()
        {
            // Act
            var response = await _client.GetAsync("/health/ready");

            // Assert
            _output.WriteLine($"Ready check status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Ready check response: {content}");
            
            // Should return either healthy or unhealthy, but not error
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task HealthCheck_LiveEndpoint_ReturnsStatus()
        {
            // Act
            var response = await _client.GetAsync("/health/live");

            // Assert
            _output.WriteLine($"Live check status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Live check response: {content}");
            
            // Should return either healthy or unhealthy, but not error
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task HealthCheck_ReturnsJsonFormat()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("application/json", response.Content.Headers.ContentType?.ToString() ?? "");
            
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Health check JSON: {content}");
            
            // Should be valid JSON
            var healthResult = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(healthResult.TryGetProperty("status", out var status));
            _output.WriteLine($"Health status: {status.GetString()}");
        }

        [Fact]
        public async Task HealthCheck_IncludesComponentDetails()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            var healthResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (healthResult.TryGetProperty("entries", out var entries))
            {
                _output.WriteLine($"Health check entries: {entries}");
                
                // Should have self check
                var entriesObj = entries.EnumerateObject().ToList();
                Assert.Contains(entriesObj, e => e.Name.Contains("self"));
                
                foreach (var entry in entriesObj)
                {
                    _output.WriteLine($"Health check entry: {entry.Name} = {entry.Value}");
                }
            }
        }

        [Fact]
        public async Task HealthCheck_DataAccess_ReportsStatus()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"DataAccess health check: {content}");
            
            // Parse JSON and look for dataaccess health check
            var healthResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (healthResult.TryGetProperty("entries", out var entries))
            {
                var entriesObj = entries.EnumerateObject().ToList();
                var dataAccessEntry = entriesObj.FirstOrDefault(e => e.Name.ToLower().Contains("dataaccess"));
                
                if (dataAccessEntry.Value.ValueKind != JsonValueKind.Undefined)
                {
                    _output.WriteLine($"DataAccess health: {dataAccessEntry.Value}");
                    
                    if (dataAccessEntry.Value.TryGetProperty("status", out var status))
                    {
                        var statusValue = status.GetString();
                        Assert.True(statusValue == "Healthy" || statusValue == "Unhealthy" || statusValue == "Degraded");
                    }
                }
            }
        }

        [Fact]
        public async Task HealthCheck_Memory_ReportsStatus()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Memory health check: {content}");
            
            // Parse JSON and look for memory health check
            var healthResult = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (healthResult.TryGetProperty("entries", out var entries))
            {
                var entriesObj = entries.EnumerateObject().ToList();
                var memoryEntry = entriesObj.FirstOrDefault(e => e.Name.ToLower().Contains("memory"));
                
                if (memoryEntry.Value.ValueKind != JsonValueKind.Undefined)
                {
                    _output.WriteLine($"Memory health: {memoryEntry.Value}");
                    
                    if (memoryEntry.Value.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("MemoryUsedMB", out var memoryUsed))
                    {
                        var memoryValue = memoryUsed.GetDouble();
                        Assert.True(memoryValue > 0);
                        _output.WriteLine($"Memory usage: {memoryValue} MB");
                    }
                }
            }
        }

        [Fact]
        public async Task HealthCheck_ResponseTime_IsReasonable()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync("/health");
            stopwatch.Stop();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Health check took too long: {stopwatch.ElapsedMilliseconds}ms");
            
            _output.WriteLine($"Health check response time: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task HealthCheck_ConcurrentRequests_HandleGracefully()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_client.GetAsync("/health"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                Assert.True(response.StatusCode == HttpStatusCode.OK || 
                           response.StatusCode == HttpStatusCode.ServiceUnavailable);
                
                var content = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Concurrent health check: {response.StatusCode}");
            }

            // Clean up
            foreach (var response in responses)
            {
                response.Dispose();
            }
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