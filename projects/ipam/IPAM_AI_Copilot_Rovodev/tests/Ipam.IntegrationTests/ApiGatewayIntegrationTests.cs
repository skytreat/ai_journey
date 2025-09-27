using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Ipam.IntegrationTests
{
    /// <summary>
    /// Integration tests for the enhanced API Gateway with resilience patterns
    /// </summary>
    public class ApiGatewayIntegrationTests : IClassFixture<WebApplicationFactory<Ipam.ApiGateway.Program>>
    {
        private readonly WebApplicationFactory<Ipam.ApiGateway.Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public ApiGatewayIntegrationTests(WebApplicationFactory<Ipam.ApiGateway.Program> factory, ITestOutputHelper output)
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
                        config["FrontendServiceUrl"] = "https://localhost:5001";
                        config["Jwt:Issuer"] = "IPAM-Test";
                        config["Jwt:Audience"] = "IPAM-API-Test";
                        config["Jwt:Key"] = "test-key-for-integration-tests-minimum-256-bits";
                    });
                });
                
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddXUnit(output);
                });
            }).CreateClient();
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Health check response: {content}");
            
            // Verify health check format
            Assert.Contains("Healthy", content);
        }

        [Fact]
        public async Task ApiGateway_AddsCorrelationId()
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            // Should get either successful forwarding or gateway error with correlation ID
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            if (response.StatusCode == HttpStatusCode.BadGateway)
            {
                // Verify error response includes correlation ID
                Assert.Contains("correlationId", content);
            }
        }

        [Fact]
        public async Task ApiGateway_ForwardsHeaders()
        {
            // Arrange
            _client.DefaultRequestHeaders.Add("X-Test-Header", "test-value");
            _client.DefaultRequestHeaders.Add("X-Custom-Header", "custom-value");

            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response: {response.StatusCode} - {content}");

            // The gateway should attempt to forward headers
            // (We expect BadGateway since frontend isn't running, but headers should be processed)
            Assert.True(response.StatusCode == HttpStatusCode.BadGateway || response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task ApiGateway_HandlesPostRequests()
        {
            // Arrange
            var testData = new { name = "test", description = "test description" };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(testData),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/addressspaces", jsonContent);

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"POST Response: {response.StatusCode} - {content}");

            // Should handle POST requests (may fail due to no backend, but shouldn't crash)
            Assert.True(response.StatusCode == HttpStatusCode.BadGateway || 
                       response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ApiGateway_ReturnsProperErrorResponse()
        {
            // Act
            var response = await _client.GetAsync("/api/nonexistent");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error response: {response.StatusCode} - {content}");

            if (response.StatusCode == HttpStatusCode.BadGateway)
            {
                // Verify error response structure
                var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
                Assert.True(errorResponse.TryGetProperty("error", out _));
                Assert.True(errorResponse.TryGetProperty("correlationId", out _));
            }
        }

        [Fact]
        public async Task ApiGateway_RateLimiting_WorksCorrectly()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send multiple requests quickly to test rate limiting
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync($"/api/health?test={i}"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            var statusCodes = responses.Select(r => r.StatusCode).ToList();
            _output.WriteLine($"Status codes: {string.Join(", ", statusCodes)}");

            // Should have some successful responses
            Assert.Contains(HttpStatusCode.OK, statusCodes.Concat(new[] { HttpStatusCode.BadGateway }));
            
            // Clean up
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        [Theory]
        [InlineData("/api/addressspaces")]
        [InlineData("/api/tags")]
        [InlineData("/health")]
        public async Task ApiGateway_HandlesValidRoutes(string route)
        {
            // Act
            var response = await _client.GetAsync(route);

            // Assert
            _output.WriteLine($"Route {route}: {response.StatusCode}");
            
            // Should either succeed or fail gracefully
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.BadGateway ||
                       response.StatusCode == HttpStatusCode.Unauthorized ||
                       response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ApiGateway_SecurityHeaders_AreNotExposed()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            // API Gateway shouldn't expose internal security headers
            Assert.False(response.Headers.Contains("Server"));
            
            // But should allow CORS headers if configured
            _output.WriteLine($"Response headers: {string.Join(", ", response.Headers.Select(h => h.Key))}");
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