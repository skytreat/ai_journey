using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Ipam.IntegrationTests
{
    public class IPAMApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public IPAMApiTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateAndGetIPAddress_ShouldReturnSuccess()
        {
            // Arrange
            var ipAddress = new { Id = "192.168.1.1", Prefix = "192.168.1.0/24", AddressSpaceId = "default" };
            var json = System.Text.Json.JsonSerializer.Serialize(ipAddress);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/ipaddresses", content);

            // Assert
            response.EnsureSuccessStatusCode();
            
            // Get the created IP address
            var getResponse = await _client.GetAsync($"/api/ipaddresses/default/192.168.1.1");
            getResponse.EnsureSuccessStatusCode();
        }
    }
}
