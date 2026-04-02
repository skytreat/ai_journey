using FundRecommendationAPI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class SystemApiTests : IAsyncLifetime
    {
        private HttpClient? _client;
        private TestServer? _server;

        public async Task InitializeAsync()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            _server = new TestServer(new WebHostBuilder()
                .UseConfiguration(configuration)
                .UseStartup<TestStartup>());

            _client = _server.CreateClient();
        }

        public async Task DisposeAsync()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
            if (_server != null)
            {
                _server.Dispose();
            }
        }

        [Fact]
        public async Task SystemStatus_ShouldReturnRunningStatus()
        {
            var response = await _client.GetAsync("/api/system/status");
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SystemUpdate_ShouldTriggerUpdate()
        {
            var request = new { type = "full" };
            var response = await _client.PostAsJsonAsync("/api/system/update", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SystemUpdateHistory_ShouldReturnHistory()
        {
            var response = await _client.GetAsync("/api/system/update-history");
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task MetaManagers_ShouldReturnManagersList()
        {
            var response = await _client.GetAsync("/api/meta/managers");
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(result);
        }
    }
}
