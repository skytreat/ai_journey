using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Ipam.Dto;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Ipam.Frontend.Tests
{
    public class AuthApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AuthApiTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task RegisterAndLogin_ReturnsAuthToken()
        {
            // Arrange
            var username = "testuser" + Guid.NewGuid().ToString().Substring(0, 4);
            var password = "Password123!";
            var registerUser = new UserDto { Username = username, Password = password };
            var loginUser = new UserDto { Username = username, Password = password };

            // Act - Register
            var registerJson = JsonConvert.SerializeObject(registerUser);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            var registerResponse = await _client.PostAsync("/api/v1/auth/register", registerContent);

            // Assert - Register
            if (!registerResponse.IsSuccessStatusCode)
            {
                var errorContent = await registerResponse.Content.ReadAsStringAsync();
                throw new Exception($"Registration failed with status code {registerResponse.StatusCode}: {errorContent}");
            }

            // Act - Login
            var loginJson = JsonConvert.SerializeObject(loginUser);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);

            // Assert - Login
            if (!loginResponse.IsSuccessStatusCode)
            {
                var errorContent = await loginResponse.Content.ReadAsStringAsync();
                throw new Exception($"Login failed with status code {loginResponse.StatusCode}: {errorContent}");
            }
            
            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
            dynamic? result = JsonConvert.DeserializeObject(loginResponseString);
            Assert.NotNull(result?.token);
        }
    }
}