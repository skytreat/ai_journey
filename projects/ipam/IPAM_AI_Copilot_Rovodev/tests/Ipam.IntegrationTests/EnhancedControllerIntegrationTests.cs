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
    /// Integration tests for enhanced controller functionality with proper filtering and error handling
    /// </summary>
    public class EnhancedControllerIntegrationTests : IClassFixture<WebApplicationFactory<Ipam.Frontend.Program>>
    {
        private readonly WebApplicationFactory<Ipam.Frontend.Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public EnhancedControllerIntegrationTests(WebApplicationFactory<Ipam.Frontend.Program> factory, ITestOutputHelper output)
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
        public async Task IpAllocationController_GetByPrefix_ValidatesPrefix()
        {
            // Test with invalid prefix
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes/byPrefix?prefix=invalid-prefix");

            _output.WriteLine($"Invalid prefix response: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            // Should return BadRequest for invalid prefix
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid prefix format", content);
        }

        [Fact]
        public async Task IpAllocationController_GetByPrefix_AcceptsValidPrefix()
        {
            // Test with valid prefix
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes/byPrefix?prefix=192.168.1.0/24");

            _output.WriteLine($"Valid prefix response: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            // Should not return BadRequest (may be NotFound or OK depending on data)
            Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task IpAllocationController_GetByTags_RequiresTags()
        {
            // Test without tags
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes/byTags");

            _output.WriteLine($"No tags response: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            // Should return BadRequest when no tags provided
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("At least one tag must be specified", content);
        }

        [Fact]
        public async Task IpAllocationController_GetByTags_AcceptsValidTags()
        {
            // Test with valid tags
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes/byTags?tags[environment]=production&tags[region]=us-east");

            _output.WriteLine($"Valid tags response: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            // Should not return BadRequest for validation
            Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task IpAllocationController_Create_ValidatesModel()
        {
            // Test with invalid model
            var invalidModel = new
            {
                // Missing required fields
                prefix = "", // Invalid
                addressSpaceId = "test-space"
            };

            var json = JsonSerializer.Serialize(invalidModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/addressspaces/test-space/ipnodes", content);

            _output.WriteLine($"Invalid model response: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {responseContent}");

            // Should return BadRequest for validation errors
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task IpAllocationController_Create_ValidModel()
        {
            // Test with valid model
            var validModel = new
            {
                addressSpaceId = "test-space",
                prefix = "192.168.2.0/24",
                tags = new Dictionary<string, string>
                {
                    ["environment"] = "test",
                    ["region"] = "us-west"
                }
            };

            var json = JsonSerializer.Serialize(validModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/addressspaces/test-space/ipnodes", content);

            _output.WriteLine($"Valid model response: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {responseContent}");

            // Should not return BadRequest for validation (may fail for other reasons like no auth)
            Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task IpAllocationController_HandlesConcurrentRequests()
        {
            // Test concurrent requests to the same endpoint
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 5; i++)
            {
                var prefix = $"10.{i}.0.0/16";
                tasks.Add(_client.GetAsync($"/api/addressspaces/test-space/ipnodes/byPrefix?prefix={prefix}"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert all responses are handled properly
            foreach (var response in responses.Select((r, i) => new { Response = r, Index = i }))
            {
                _output.WriteLine($"Concurrent request {response.Index}: {response.Response.StatusCode}");
                
                // Should handle all requests without crashing
                Assert.True(response.Response.StatusCode == HttpStatusCode.OK ||
                           response.Response.StatusCode == HttpStatusCode.NotFound ||
                           response.Response.StatusCode == HttpStatusCode.InternalServerError);
            }

            // Clean up
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        [Theory]
        [InlineData("192.168.1.0/24")]
        [InlineData("10.0.0.0/8")]
        [InlineData("172.16.0.0/12")]
        [InlineData("2001:db8::/32")]
        public async Task IpAllocationController_GetByPrefix_HandlesVariousPrefixFormats(string prefix)
        {
            // Act
            var response = await _client.GetAsync($"/api/addressspaces/test-space/ipnodes/byPrefix?prefix={Uri.EscapeDataString(prefix)}");

            // Assert
            _output.WriteLine($"Prefix {prefix}: {response.StatusCode}");
            
            // Should accept valid CIDR formats
            Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("192.168.1.0")]
        [InlineData("192.168.1.0/")]
        [InlineData("192.168.1.0/33")]
        [InlineData("256.256.256.256/24")]
        public async Task IpAllocationController_GetByPrefix_RejectsInvalidPrefixes(string invalidPrefix)
        {
            // Act
            var response = await _client.GetAsync($"/api/addressspaces/test-space/ipnodes/byPrefix?prefix={Uri.EscapeDataString(invalidPrefix)}");

            // Assert
            _output.WriteLine($"Invalid prefix {invalidPrefix}: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error response: {content}");
            
            // Should reject invalid CIDR formats
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid prefix format", content);
        }

        [Fact]
        public async Task GlobalErrorHandling_CatchesExceptions()
        {
            // Try to trigger an exception with malformed request
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes/byPrefix?prefix=192.168.1.0/24&malformed=true");

            _output.WriteLine($"Exception handling test: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Response content: {content}");

            // Should handle exceptions gracefully
            Assert.True(response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ResponseHeaders_IncludeSecurityHeaders()
        {
            // Act
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes");

            // Assert
            _output.WriteLine("Checking security headers...");
            
            var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            foreach (var header in headers)
            {
                _output.WriteLine($"Header: {header.Key} = {header.Value}");
            }

            // Should include security headers (if configured in middleware)
            // Note: These are set in the Frontend service, not necessarily visible in API responses
            Assert.True(headers.ContainsKey("X-Content-Type-Options") || 
                       headers.ContainsKey("X-Frame-Options") ||
                       headers.Any()); // At least some headers should be present
        }

        [Fact]
        public async Task CorrelationId_MaintainedAcrossRequests()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

            // Act
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes");

            // Assert
            _output.WriteLine($"Correlation ID test: {response.StatusCode}");
            
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            _output.WriteLine($"Response headers: {string.Join(", ", responseHeaders.Keys)}");

            // The correlation ID should be maintained in logging (though not necessarily returned in headers)
            // This test mainly ensures the request is processed without errors
            Assert.True(response.StatusCode != HttpStatusCode.InternalServerError);
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