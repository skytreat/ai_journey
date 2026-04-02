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
    public class FavoritesApiTests : IAsyncLifetime
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
        public async Task GetFavorites_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("favorites", out _));
        }

        [Fact]
        public async Task GetFavorites_WithUserId_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites?userId=user123");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddFavorite_ReturnsOk()
        {
            // Arrange
            var request = new {
                Code = "000001",
                UserId = "user123",
                Note = "长期持有"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/favorites", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task RemoveFavorite_ReturnsOk()
        {
            // Act
            var response = await _client.DeleteAsync("/api/favorites/000001");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task RemoveFavorite_WithUserId_ReturnsOk()
        {
            // Act
            var response = await _client.DeleteAsync("/api/favorites/000001?userId=user123");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task UpdateFavoritesSort_ReturnsOk()
        {
            // Arrange
            var request = new {
                FundCodes = new[] { "000001", "000002", "000003" },
                UserId = "user123"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/favorites/sort", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task UpdateFavoriteNote_ReturnsOk()
        {
            // Arrange
            var request = new {
                Note = "短期持有"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/favorites/000001/note", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task GetFavoriteGroups_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/groups");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("groups", out _));
        }

        [Fact]
        public async Task GetFavoriteGroups_WithUserId_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/favorites/groups?userId=user123");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateFavoriteGroup_ReturnsOk()
        {
            // Arrange
            var request = new {
                Name = "高收益基金",
                UserId = "user123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/favorites/groups", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }

        [Fact]
        public async Task MoveFundToGroup_ReturnsOk()
        {
            // Arrange
            var request = new {
                GroupId = "1",
                UserId = "user123"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/favorites/000001/group", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotNull(result);
            Assert.True(result.TryGetProperty("success", out var successElement) && successElement.GetBoolean());
        }
    }
}
