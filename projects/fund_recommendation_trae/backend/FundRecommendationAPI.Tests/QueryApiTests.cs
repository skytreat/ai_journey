using FundRecommendationAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class QueryApiTests : IAsyncLifetime
    {
        private HttpClient _client;
        private IHost _host;

        public async Task InitializeAsync()
        {
            // 创建测试主机
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            var startup = new ProgramIntegrationTestsWithFactory.TestStartup();
                            startup.ConfigureServices(services);
                        })
                        .Configure(app =>
                        {
                            var startup = new ProgramIntegrationTestsWithFactory.TestStartup();
                            startup.Configure(app, null);
                        });
                });
            
            // 构建并启动主机
            _host = await hostBuilder.StartAsync();
            _client = _host.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            if (_host != null)
            {
                _host.Dispose();
            }
            await Task.CompletedTask;
        }

        [Fact]
        public async Task ExecuteKqlQuery_ReturnsOk()
        {
            // Arrange
            var request = new {
                Query = "fundType='混合型' and returnRate > 0.1",
                UserId = "user123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/query/kql", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("query", out _));
            Assert.True(result.TryGetProperty("results", out _));
            Assert.True(result.TryGetProperty("totalCount", out _));
        }

        [Fact]
        public async Task GetQueryHistory_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/query/history");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("history", out _));
        }

        [Fact]
        public async Task GetQueryHistory_WithParameters_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/query/history?userId=user123&limit=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SaveQueryTemplate_ReturnsOk()
        {
            // Arrange
            var request = new {
                Name = "高收益混合型基金",
                Query = "fundType='混合型' and returnRate > 0.1",
                UserId = "user123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/query/templates", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task GetQueryTemplates_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/query/templates");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("templates", out _));
        }

        [Fact]
        public async Task GetQueryTemplates_WithUserId_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/query/templates?userId=user123");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }
    }
}
