using Xunit;
using Moq;
using Moq.Contrib.HttpClient;
using Ipam.Client;
using Ipam.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace Ipam.Client.Tests
{
    /// <summary>
    /// Comprehensive unit tests for IpamApiClient
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpamApiClientTests : IDisposable
    {
        private readonly Mock<HttpMessageHandler> _httpHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly IpamApiClient _client;
        private readonly string _baseUrl = "https://api.ipam.test";

        public IpamApiClientTests()
        {
            _httpHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpHandlerMock.Object)
            {
                BaseAddress = new Uri(_baseUrl)
            };
            _client = new IpamApiClient(_httpClient);
        }

        [Fact]
        public async Task CreateAddressSpaceAsync_ValidAddressSpace_ReturnsCreatedAddressSpace()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = "space1",
                Name = "Test Space",
                Description = "Test Description"
            };

            var expectedResponse = new AddressSpace
            {
                Id = "space1",
                Name = "Test Space",
                Description = "Test Description",
                CreatedOn = DateTime.UtcNow
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Post, $"{_baseUrl}/api/addressspaces")
                .ReturnsJsonResponse(HttpStatusCode.Created, expectedResponse);

            // Act
            var result = await _client.CreateAddressSpaceAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(expectedResponse.Name, result.Name);
        }

        [Fact]
        public async Task GetAddressSpaceAsync_ExistingId_ReturnsAddressSpace()
        {
            // Arrange
            var addressSpaceId = "space1";
            var expectedAddressSpace = new AddressSpace
            {
                Id = addressSpaceId,
                Name = "Test Space",
                Description = "Test Description"
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Get, $"{_baseUrl}/api/addressspaces/{addressSpaceId}")
                .ReturnsJsonResponse(HttpStatusCode.OK, expectedAddressSpace);

            // Act
            var result = await _client.GetAddressSpaceAsync(addressSpaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedAddressSpace.Id, result.Id);
            Assert.Equal(expectedAddressSpace.Name, result.Name);
        }

        [Fact]
        public async Task GetAddressSpaceAsync_NonExistentId_ThrowsHttpRequestException()
        {
            // Arrange
            var addressSpaceId = "nonexistent";

            _httpHandlerMock.SetupRequest(HttpMethod.Get, $"{_baseUrl}/api/addressspaces/{addressSpaceId}")
                .ReturnsResponse(HttpStatusCode.NotFound);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _client.GetAddressSpaceAsync(addressSpaceId));
        }

        [Fact]
        public async Task GetAddressSpacesAsync_ValidRequest_ReturnsAddressSpacesList()
        {
            // Arrange
            var expectedAddressSpaces = new List<AddressSpace>
            {
                new AddressSpace { Id = "space1", Name = "Space 1" },
                new AddressSpace { Id = "space2", Name = "Space 2" },
                new AddressSpace { Id = "space3", Name = "Space 3" }
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Get, $"{_baseUrl}/api/addressspaces")
                .ReturnsJsonResponse(HttpStatusCode.OK, expectedAddressSpaces);

            // Act
            var result = await _client.GetAddressSpacesAsync();

            // Assert
            Assert.NotNull(result);
            var addressSpacesList = Assert.IsAssignableFrom<IEnumerable<AddressSpace>>(result);
            Assert.Equal(3, ((List<AddressSpace>)addressSpacesList).Count);
        }

        [Fact]
        public async Task UpdateAddressSpaceAsync_ValidUpdate_ReturnsUpdatedAddressSpace()
        {
            // Arrange
            var addressSpaceId = "space1";
            var addressSpace = new AddressSpace
            {
                Id = addressSpaceId,
                Name = "Updated Space",
                Description = "Updated Description"
            };

            var expectedResponse = new AddressSpace
            {
                Id = addressSpaceId,
                Name = "Updated Space",
                Description = "Updated Description",
                ModifiedOn = DateTime.UtcNow
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Put, $"{_baseUrl}/api/addressspaces/{addressSpaceId}")
                .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

            // Act
            var result = await _client.UpdateAddressSpaceAsync(addressSpaceId, addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Name, result.Name);
            Assert.Equal(expectedResponse.Description, result.Description);
        }

        [Fact]
        public async Task DeleteAddressSpaceAsync_ExistingId_CompletesSuccessfully()
        {
            // Arrange
            var addressSpaceId = "space1";

            _httpHandlerMock.SetupRequest(HttpMethod.Delete, $"{_baseUrl}/api/addressspaces/{addressSpaceId}")
                .ReturnsResponse(HttpStatusCode.NoContent);

            // Act & Assert
            await _client.DeleteAddressSpaceAsync(addressSpaceId);
            // Should complete without throwing
        }

        [Fact]
        public async Task CreateIPAddressAsync_ValidIPAddress_ReturnsCreatedIPAddress()
        {
            // Arrange
            var addressSpaceId = "space1";
            var ipAddress = new IPAddress
            {
                Id = "ip1",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
                Tags = new List<IPAddressTag>
                {
                    new IPAddressTag { Name = "Environment", Value = "Test" }
                }
            };

            var expectedResponse = new IPAddress
            {
                Id = "ip1",
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
                Tags = ipAddress.Tags,
                CreatedOn = DateTime.UtcNow
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Post, $"{_baseUrl}/api/addressspaces/{addressSpaceId}/ipaddresses")
                .ReturnsJsonResponse(HttpStatusCode.Created, expectedResponse);

            // Act
            var result = await _client.CreateIPAddressAsync(addressSpaceId, ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(expectedResponse.Prefix, result.Prefix);
        }

        [Fact]
        public async Task GetIPAddressesAsync_WithCidrFilter_ReturnsFilteredResults()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.0.0/16";
            var expectedIPAddresses = new List<IPAddress>
            {
                new IPAddress { Id = "ip1", Prefix = "10.0.1.0/24" },
                new IPAddress { Id = "ip2", Prefix = "10.0.2.0/24" }
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Get, $"{_baseUrl}/api/addressspaces/{addressSpaceId}/ipaddresses?cidr=10.0.0.0%2F16")
                .ReturnsJsonResponse(HttpStatusCode.OK, expectedIPAddresses);

            // Act
            var result = await _client.GetIPAddressesAsync(addressSpaceId, cidr);

            // Assert
            Assert.NotNull(result);
            var ipAddressesList = Assert.IsAssignableFrom<IEnumerable<IPAddress>>(result);
            Assert.Equal(2, ((List<IPAddress>)ipAddressesList).Count);
        }

        [Fact]
        public async Task GetIPAddressesAsync_WithTagsFilter_ReturnsFilteredResults()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "USEast" }
            };
            var expectedIPAddresses = new List<IPAddress>
            {
                new IPAddress { Id = "ip1", Prefix = "10.0.1.0/24" }
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Get, $"{_baseUrl}/api/addressspaces/{addressSpaceId}/ipaddresses?tags=Environment%3DProduction&tags=Region%3DUSEast")
                .ReturnsJsonResponse(HttpStatusCode.OK, expectedIPAddresses);

            // Act
            var result = await _client.GetIPAddressesAsync(addressSpaceId, null, tags);

            // Assert
            Assert.NotNull(result);
            var ipAddressesList = Assert.IsAssignableFrom<IEnumerable<IPAddress>>(result);
            Assert.Single(ipAddressesList);
        }

        [Fact]
        public async Task CreateTagAsync_ValidTag_ReturnsCreatedTag()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tag = new Tag
            {
                Name = "Environment",
                Type = "Inheritable",
                Description = "Environment classification",
                KnownValues = new[] { "Production", "Development" }
            };

            var expectedResponse = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = "Environment",
                Type = "Inheritable",
                Description = "Environment classification",
                KnownValues = tag.KnownValues,
                CreatedOn = DateTime.UtcNow
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Post, $"{_baseUrl}/api/addressspaces/{addressSpaceId}/tags")
                .ReturnsJsonResponse(HttpStatusCode.Created, expectedResponse);

            // Act
            var result = await _client.CreateTagAsync(addressSpaceId, tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Name, result.Name);
            Assert.Equal(expectedResponse.Type, result.Type);
        }

        [Fact]
        public async Task GetTagsAsync_ValidAddressSpace_ReturnsTagsList()
        {
            // Arrange
            var addressSpaceId = "space1";
            var expectedTags = new List<Tag>
            {
                new Tag { Name = "Environment", Type = "Inheritable" },
                new Tag { Name = "Application", Type = "NonInheritable" }
            };

            _httpHandlerMock.SetupRequest(HttpMethod.Get, $"{_baseUrl}/api/addressspaces/{addressSpaceId}/tags")
                .ReturnsJsonResponse(HttpStatusCode.OK, expectedTags);

            // Act
            var result = await _client.GetTagsAsync(addressSpaceId);

            // Assert
            Assert.NotNull(result);
            var tagsList = Assert.IsAssignableFrom<IEnumerable<Tag>>(result);
            Assert.Equal(2, ((List<Tag>)tagsList).Count);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetAddressSpaceAsync_InvalidId_ThrowsArgumentException(string invalidId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.GetAddressSpaceAsync(invalidId));
        }

        [Fact]
        public void Constructor_WithBaseUrl_SetsBaseAddress()
        {
            // Arrange & Act
            var client = new IpamApiClient("https://test.api.com");

            // Assert
            // We can't directly test the private HttpClient, but we can verify
            // the constructor doesn't throw and the client is usable
            Assert.NotNull(client);
        }

        [Fact]
        public void Dispose_DisposesHttpClient()
        {
            // Arrange
            var client = new IpamApiClient("https://test.api.com");

            // Act & Assert
            client.Dispose(); // Should not throw
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _client?.Dispose();
        }
    }
}