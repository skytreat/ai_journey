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
    public class AnalysisApiTests : IAsyncLifetime
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
        public async Task GetFundRanking_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/ranking");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("period", out _));
            Assert.True(result.TryGetProperty("limit", out _));
            Assert.True(result.TryGetProperty("order", out _));
            Assert.True(result.TryGetProperty("rankings", out _));
        }

        [Fact]
        public async Task GetFundRanking_WithParameters_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/ranking?period=year&limit=5&order=asc");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/change");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("period", out _));
            Assert.True(result.TryGetProperty("limit", out _));
            Assert.True(result.TryGetProperty("type", out _));
            Assert.True(result.TryGetProperty("rankings", out _));
        }

        [Fact]
        public async Task GetFundChangeRanking_WithParameters_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/change?period=quarter&limit=5&type=relative");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/consistency");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("startDate", out _));
            Assert.True(result.TryGetProperty("endDate", out _));
            Assert.True(result.TryGetProperty("limit", out _));
            Assert.True(result.TryGetProperty("funds", out _));
        }

        [Fact]
        public async Task GetFundConsistency_WithParameters_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/consistency?startDate=2023-01-01&endDate=2023-12-31&limit=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/multifactor");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("limit", out _));
            Assert.True(result.TryGetProperty("funds", out _));
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithParameters_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/analysis/multifactor?limit=5&factors=return,risk");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CompareFunds_ReturnsOk()
        {
            // Arrange
            var request = new {
                FundIds = new[] { "000001", "000002" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/analysis/compare", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("funds", out _));
        }
    }
}
