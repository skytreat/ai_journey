using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using IPAM.Core;
using IPAM.Data;
using Moq;
using Xunit;

namespace IPAM.API.Tests.Repositories
{
    public class AzureTableRepositoryTests
    {
        private readonly Mock<TableServiceClient> _mockTableServiceClient;
        private readonly AzureTableRepository _repository;

        public AzureTableRepositoryTests()
        {
            _mockTableServiceClient = new Mock<TableServiceClient>();
            _repository = new AzureTableRepository("UseDevelopmentStorage=true");
            
            // 使用反射设置私有字段_tableServiceClient以进行测试
            var fieldInfo = typeof(AzureTableRepository).GetField("_tableServiceClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(_repository, _mockTableServiceClient.Object);
        }

        [Fact]
        public async Task InitializeAsync_ShouldCreateTables()
        {
            // Arrange
            var mockTableClient = new Mock<TableClient>();
            _mockTableServiceClient.Setup(x => x.GetTableClient(It.IsAny<string>())).Returns(mockTableClient.Object);

            // Act
            await _repository.InitializeAsync();

            // Assert
            _mockTableServiceClient.Verify(x => x.CreateTableIfNotExistsAsync("AddressSpaces"), Times.Once);
            _mockTableServiceClient.Verify(x => x.CreateTableIfNotExistsAsync("Tags"), Times.Once);
            _mockTableServiceClient.Verify(x => x.CreateTableIfNotExistsAsync("IPs"), Times.Once);
        }

        [Fact]
        public async Task GetAddressSpaceById_ShouldReturnAddressSpace()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var mockTableClient = new Mock<TableClient>();
            var testEntity = new TableEntity(testId.ToString(), testId.ToString())
            {
                {"Name", "Test"},
                {"Description", "Test Description"},
                {"CreatedOn", DateTime.UtcNow},
                {"ModifiedOn", DateTime.UtcNow}
            };

            // 模拟TableServiceClientFactory
            _mockTableServiceClient.Setup(x => x.GetTableClient("AddressSpaces")).Returns(mockTableClient.Object);
            mockTableClient.Setup(x => x.GetEntityAsync<TableEntity>(testId.ToString(), testId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(testEntity, Mock.Of<Response>()));

            // Act
            var result = await _repository.GetAddressSpaceById(testId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testId, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task AddAddressSpace_ShouldCallAddEntityAsync()
        {
            // Arrange
            var testAddressSpace = new AddressSpace
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Description = "Test Description",
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            var mockTableClient = new Mock<TableClient>();
            _mockTableServiceClient.Setup(x => x.GetTableClient("AddressSpaces")).Returns(mockTableClient.Object);

            // Act
            await _repository.AddAddressSpace(testAddressSpace);

            // Assert
            mockTableClient.Verify(x => x.AddEntityAsync(
                It.Is<TableEntity>(e => 
                    e.PartitionKey == testAddressSpace.Id.ToString() && 
                    e.RowKey == testAddressSpace.Id.ToString()),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetIpById_ShouldReturnIp()
        {
            // Arrange
            var addressSpaceId = Guid.NewGuid();
            var ipId = Guid.NewGuid();
            var mockTableClient = new Mock<TableClient>();
            var testEntity = new TableEntity(addressSpaceId.ToString(), ipId.ToString())
            {
                {"Prefix", "192.168.1.1/24"},
                {"Status", "Available"},
                {"CreatedOn", DateTime.UtcNow}
            };

            _mockTableServiceClient.Setup(x => x.GetTableClient("IPs")).Returns(mockTableClient.Object);
            mockTableClient.Setup(x => x.GetEntityAsync<TableEntity>(addressSpaceId.ToString(), ipId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(testEntity, Mock.Of<Response>()));

            // Act
            var result = await _repository.GetIpById(addressSpaceId, ipId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipId, result.Id);
            Assert.Equal("192.168.1.1/24", result.Prefix);
        }

        [Fact]
        public async Task AddTag_ShouldCallAddEntityAsync()
        {
            // Arrange
            var addressSpaceId = Guid.NewGuid();
            var testTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = "TestTag",
                CreatedOn = DateTime.UtcNow
            };

            var mockTableClient = new Mock<TableClient>();
            _mockTableServiceClient.Setup(x => x.GetTableClient("Tags")).Returns(mockTableClient.Object);

            // Act
            await _repository.AddTag(testTag);

            // Assert
            mockTableClient.Verify(x => x.AddEntityAsync(
                It.Is<TableEntity>(e => 
                    e.PartitionKey == addressSpaceId.ToString() && 
                    e.RowKey == "TestTag"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}