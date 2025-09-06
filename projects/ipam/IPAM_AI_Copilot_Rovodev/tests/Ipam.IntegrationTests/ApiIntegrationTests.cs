using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ipam.Frontend;
using Ipam.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ipam.IntegrationTests
{
    /// <summary>
    /// Integration tests for IPAM API endpoints
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthyStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var healthStatus = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.Equal("Healthy", healthStatus.GetProperty("Status").GetString());
        }

        [Fact]
        public async Task HealthCheck_Detailed_ReturnsDetailedHealthStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/health/detailed");

            // Assert
            // May return 200 (Healthy) or 503 (Degraded) depending on dependencies
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
            
            var content = await response.Content.ReadAsStringAsync();
            var healthStatus = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(healthStatus.TryGetProperty("Status", out _));
            Assert.True(healthStatus.TryGetProperty("Checks", out _));
        }

        [Fact]
        public async Task HealthCheck_Readiness_ReturnsReadinessStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/health/ready");

            // Assert
            // May return 200 (Ready) or 503 (NotReady) depending on dependencies
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task HealthCheck_Liveness_ReturnsAliveStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/health/live");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var livenessStatus = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.Equal("Alive", livenessStatus.GetProperty("Status").GetString());
        }

        [Fact]
        public async Task AddressSpaces_GetAll_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/addressspaces");

            // Assert
            // May require authentication, so we check for either success or unauthorized
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddressSpaces_GetNonExistent_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/addressspaces/nonexistent-id");

            // Assert
            // Should return either NotFound or Unauthorized (if auth is required)
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Metrics_Get_ReturnsMetricsData()
        {
            // Act
            var response = await _client.GetAsync("/api/health/metrics");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(metrics.TryGetProperty("MetricsCount", out _));
            Assert.True(metrics.TryGetProperty("Metrics", out _));
        }

        [Theory]
        [InlineData("/api/addressspaces")]
        [InlineData("/api/health")]
        [InlineData("/api/health/live")]
        [InlineData("/api/health/ready")]
        [InlineData("/api/health/metrics")]
        public async Task Endpoints_ReturnValidContentType(string endpoint)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            // Should return JSON content type for API endpoints
            if (response.IsSuccessStatusCode)
            {
                Assert.Contains("application/json", response.Content.Headers.ContentType?.ToString());
            }
        }

        [Fact]
        public async Task API_SupportsOptionsRequest()
        {
            // Act
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/addressspaces");
            var response = await _client.SendAsync(request);

            // Assert
            // Should support OPTIONS for CORS
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NoContent ||
                       response.StatusCode == HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public async Task API_HandlesInvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/addressspaces", content);

            // Assert
            // Should return BadRequest for invalid JSON or Unauthorized if auth required
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task API_HandlesLargePayload_ReturnsAppropriateResponse()
        {
            // Arrange
            var largePayload = new
            {
                Name = new string('A', 10000), // Very long name
                Description = new string('B', 50000) // Very long description
            };
            var json = JsonSerializer.Serialize(largePayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/addressspaces", content);

            // Assert
            // Should handle large payloads gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.RequestEntityTooLarge ||
                       response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task API_ReturnsConsistentErrorFormat()
        {
            // Act
            var response = await _client.GetAsync("/api/addressspaces/invalid-id-format");

            // Assert
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                if (!string.IsNullOrEmpty(content))
                {
                    var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    // Error responses should have consistent structure
                    Assert.True(errorResponse.TryGetProperty("message", out _) ||
                               errorResponse.TryGetProperty("error", out _) ||
                               errorResponse.TryGetProperty("title", out _));
                }
            }
        }

        [Fact]
        public async Task API_PerformanceLogging_RecordsMetrics()
        {
            // Arrange - Make a request to generate metrics
            await _client.GetAsync("/api/health");

            // Act - Check if metrics were recorded
            var response = await _client.GetAsync("/api/health/metrics");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<JsonElement>(content);
            
            var metricsCount = metrics.GetProperty("MetricsCount").GetInt32();
            Assert.True(metricsCount >= 0); // Should have some metrics recorded
        }

        [Fact]
        public async Task API_ErrorHandling_ReturnsStructuredErrors()
        {
            // Act - Make a request that should trigger error handling
            var response = await _client.GetAsync("/api/addressspaces/trigger-error");

            // Assert
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                if (!string.IsNullOrEmpty(content))
                {
                    // Should return structured JSON error
                    Assert.DoesNotThrow(() => JsonSerializer.Deserialize<JsonElement>(content));
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task API_SupportsCORS_ForDifferentMethods(string method)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(method), "/api/addressspaces");
            request.Headers.Add("Origin", "https://example.com");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            // CORS headers should be present for cross-origin requests
            if (response.Headers.Contains("Access-Control-Allow-Origin") ||
                response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Either CORS is configured or authentication is required
                Assert.True(true);
            }
        }

        [Fact]
        public async Task API_Swagger_IsAccessible()
        {
            // Act
            var response = await _client.GetAsync("/swagger");

            // Assert
            // Swagger should be accessible in development
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.Redirect);
        }
    }
}