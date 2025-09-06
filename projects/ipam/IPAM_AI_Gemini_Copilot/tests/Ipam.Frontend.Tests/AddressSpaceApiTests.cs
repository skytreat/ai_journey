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
    public class AddressSpaceApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private string _authToken;

        public AddressSpaceApiTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _authToken = GetAuthToken().GetAwaiter().GetResult();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        }

        private async Task<string> GetAuthToken()
        {
            // Register a test user
            var registerUser = new UserDto { Username = "testuser" + Guid.NewGuid().ToString().Substring(0, 4), Password = "Password123!" };
            var registerJson = JsonConvert.SerializeObject(registerUser);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            var registerResponse = await _client.PostAsync("/api/v1/auth/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            // Login with the test user
            var loginUser = new UserDto { Username = registerUser.Username, Password = registerUser.Password };
            var loginJson = JsonConvert.SerializeObject(loginUser);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/v1/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            dynamic? result = JsonConvert.DeserializeObject(loginResponseString);
            if (result == null || result.token == null)
            {
                throw new InvalidOperationException("Login response or token was null.");
            }
            return (string)result.token!;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        [Fact]
        public async Task GetAddressSpaces_ReturnsSuccessAndCorrectContentType()
        {
            // Arrange

            // Act
            var response = await _client.GetAsync("/api/v1/addressspaces");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());
        }

        [Fact]
        public async Task CreateAddressSpace_ReturnsCreatedAddressSpace()
        {
            // Arrange
            var newAddressSpace = new AddressSpaceDto
            {
                Name = "TestAddressSpace" + Guid.NewGuid().ToString().Substring(0, 5),
                Description = "Description for test address space"
            };
            var json = JsonConvert.SerializeObject(newAddressSpace);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/addressspaces", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var createdAddressSpace = JsonConvert.DeserializeObject<AddressSpaceDto>(responseString);

            Assert.NotNull(createdAddressSpace);
            Assert.Equal(newAddressSpace.Name, createdAddressSpace.Name);
            Assert.Equal(newAddressSpace.Description, createdAddressSpace.Description);
        }
    }
}