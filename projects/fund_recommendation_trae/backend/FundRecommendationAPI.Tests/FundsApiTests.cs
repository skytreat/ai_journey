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
    public class FundsApiTests : IAsyncLifetime
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
        public async Task GetFundsList_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("total", out _));
            Assert.True(result.TryGetProperty("page", out _));
            Assert.True(result.TryGetProperty("pageSize", out _));
            Assert.True(result.TryGetProperty("funds", out _));
        }

        [Fact]
        public async Task GetFundsList_WithFundType_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds?fundType=混合型");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundsList_WithRiskLevel_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds?riskLevel=中风险");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundDetail_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds/000001");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("code", out _));
            Assert.True(result.TryGetProperty("name", out _));
        }

        [Fact]
        public async Task GetFundDetail_NotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/funds/999999");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetFundNav_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds/000001/nav");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("code", out _));
            Assert.True(result.TryGetProperty("navHistory", out _));
        }

        [Fact]
        public async Task GetFundNav_WithDateRange_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds/000001/nav?startDate=2024-01-01&endDate=2024-01-31");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundPerformance_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds/000001/performance");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("code", out _));
            Assert.True(result.TryGetProperty("performances", out _));
        }

        [Fact]
        public async Task GetFundManagers_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds/000001/managers");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("code", out _));
            Assert.True(result.TryGetProperty("managers", out _));
        }

        [Fact]
        public async Task GetFundScale_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/funds/000001/scale");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("code", out _));
            Assert.True(result.TryGetProperty("scales", out _));
        }
    }
}
