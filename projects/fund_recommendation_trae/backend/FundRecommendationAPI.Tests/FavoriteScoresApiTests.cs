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
    public class FavoriteScoresApiTests : IAsyncLifetime
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
        public async Task GetFavoriteScores_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/scores");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("scores", out _));
        }

        [Fact]
        public async Task GetFavoriteScores_WithUserId_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/scores?userId=user123");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CalculateScores_ReturnsOk()
        {
            // Arrange
            var request = new {
                FundCodes = new[] { "000001", "000002" },
                UserId = "user123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/favorites/scores/calculate", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task GetScoreHistory_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/scores/history?code=000001");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("code", out _));
            Assert.True(result.TryGetProperty("history", out _));
        }

        [Fact]
        public async Task GetScoreHistory_WithUserId_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/scores/history?code=000001&userId=user123");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetWeights_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/scores/weights");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("returnWeight", out _));
            Assert.True(result.TryGetProperty("riskWeight", out _));
            Assert.True(result.TryGetProperty("riskAdjustedReturnWeight", out _));
            Assert.True(result.TryGetProperty("rankingWeight", out _));
        }

        [Fact]
        public async Task GetWeights_WithUserId_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/scores/weights?userId=user123");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateWeights_ReturnsOk()
        {
            // Arrange
            var request = new {
                ReturnWeight = 0.3,
                RiskWeight = 0.2,
                RiskAdjustedReturnWeight = 0.3,
                RankingWeight = 0.2,
                UserId = "user123"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/favorites/scores/weights", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }
    }
}
