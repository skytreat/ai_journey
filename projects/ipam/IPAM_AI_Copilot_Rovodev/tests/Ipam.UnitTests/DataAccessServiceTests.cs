using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Moq;
using Xunit;
using Azure.Data.Tables;
using System.Linq;

namespace Ipam.UnitTests
{
    public class DataAccessServiceTests
    {
        [Fact]
        public async Task CreateIPAddressAsync_WithValidIpAllocation_ReturnsCreatedIpAllocation()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var ipAllocation = new IpAllocation
            {
                Id = "192.168.1.1",
                Prefix = "192.168.1.0/24",
                AddressSpaceId = "default",
                ParentId = null
            };
            
            mockTableServiceClient.Setup(client => client.GetTableClient("IPAddresses"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.AddEntityAsync(It.IsAny<TableEntity>()))
                .ReturnsAsync(new Response());

            // Act
            var result = await dataAccessService.CreateIPAddressAsync(ipAllocation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipAllocation.Id, result.Id);
            Assert.Equal(ipAllocation.Prefix, result.Prefix);
            mockTableServiceClient.Verify(client => client.GetTableClient("IPAddresses"), Times.Once);
            mockTableClient.Verify(client => client.AddEntityAsync(It.IsAny<TableEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddressAsync_WithExistingId_ReturnsIPAddress()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var ipAddress = new IPAddress
            {
                Id = "192.168.1.1",
                Prefix = "192.168.1.0/24",
                AddressSpaceId = "default",
                ParentId = null
            };
            
            var tableEntity = new TableEntity("default", "192.168.1.1");
            tableEntity["Prefix"] = "192.168.1.0/24";
            tableEntity["Tags"] = "[]";
            tableEntity["CreatedOn"] = DateTimeOffset.UtcNow;
            tableEntity["ModifiedOn"] = DateTimeOffset.UtcNow;
            tableEntity["ParentId"] = null;
            tableEntity["AddressSpaceId"] = "default";
            
            mockTableServiceClient.Setup(client => client.GetTableClient("IPAddresses"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.GetEntityAsync<TableEntity>("default", "192.168.1.1", null))
                .ReturnsAsync(Response.FromValue(tableEntity, new Mock<Response>().Object));

            // Act
            var result = await dataAccessService.GetIPAddressAsync("default", "192.168.1.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ipAddress.Id, result.Id);
            Assert.Equal(ipAddress.Prefix, result.Prefix);
            mockTableServiceClient.Verify(client => client.GetTableClient("IPAddresses"), Times.Once);
            mockTableClient.Verify(client => client.GetEntityAsync<TableEntity>("default", "192.168.1.1", null), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddressAsync_WithNonExistingId_ReturnsNull()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            mockTableServiceClient.Setup(client => client.GetTableClient("IPAddresses"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.GetEntityAsync<TableEntity>("default", "192.168.1.2", null))
                .Throws(new RequestFailedException("Entity not found"));

            // Act
            var result = await dataAccessService.GetIPAddressAsync("default", "192.168.1.2");

            // Assert
            Assert.Null(result);
            mockTableServiceClient.Verify(client => client.GetTableClient("IPAddresses"), Times.Once);
            mockTableClient.Verify(client => client.GetEntityAsync<TableEntity>("default", "192.168.1.2", null), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddressesAsync_ReturnsAllIPAddresses()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var tableEntities = new List<TableEntity>
            {
                new TableEntity("default", "192.168.1.1")
                {
                    ["Prefix"] = "192.168.1.0/24",
                    ["Tags"] = "[]",
                    ["CreatedOn"] = DateTimeOffset.UtcNow,
                    ["ModifiedOn"] = DateTimeOffset.UtcNow,
                    ["ParentId"] = null,
                    ["AddressSpaceId"] = "default"
                },
                new TableEntity("default", "192.168.1.2")
                {
                    ["Prefix"] = "192.168.1.0/24",
                    ["Tags"] = "[]",
                    ["CreatedOn"] = DateTimeOffset.UtcNow,
                    ["ModifiedOn"] = DateTimeOffset.UtcNow,
                    ["ParentId"] = null,
                    ["AddressSpaceId"] = "default"
                }
            };
            
            var page = Page<TableEntity>.FromValues(tableEntities, null, new Mock<Response>().Object);
            var pages = new List<Page<TableEntity>> { page };
            var mockAsyncPageable = new Mock<AsyncPageable<TableEntity>>();
            
            mockTableServiceClient.Setup(client => client.GetTableClient("IPAddresses"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, null))
                .Returns(mockAsyncPageable.Object);
            
            // Setup the mock to return our pages
            mockAsyncPageable.Setup(p => p.AsPages(It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(pages);

            // Act
            var result = await dataAccessService.GetIPAddressesAsync("default");

            // Assert
            Assert.NotNull(result);
            var ipAddresses = result.ToList();
            Assert.Equal(2, ipAddresses.Count);
            Assert.Equal("192.168.1.1", ipAddresses[0].Id);
            Assert.Equal("192.168.1.2", ipAddresses[1].Id);
            mockTableServiceClient.Verify(client => client.GetTableClient("IPAddresses"), Times.Once);
            mockTableClient.Verify(client => client.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, null), Times.Once);
        }
        
        [Fact]
        public async Task CreateAddressSpaceAsync_WithValidAddressSpace_ReturnsCreatedAddressSpace()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var addressSpace = new AddressSpace
            {
                Id = "test-id",
                Name = "Test Address Space",
                Description = "Test Description"
            };
            
            mockTableServiceClient.Setup(client => client.GetTableClient("AddressSpaces"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.AddEntityAsync(It.IsAny<TableEntity>()))
                .ReturnsAsync(new Response());

            // Act
            var result = await dataAccessService.CreateAddressSpaceAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpace.Id, result.Id);
            Assert.Equal(addressSpace.Name, result.Name);
            mockTableServiceClient.Verify(client => client.GetTableClient("AddressSpaces"), Times.Once);
            mockTableClient.Verify(client => client.AddEntityAsync(It.IsAny<TableEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task GetAddressSpaceAsync_WithExistingId_ReturnsAddressSpace()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var addressSpace = new AddressSpace
            {
                Id = "test-id",
                Name = "Test Address Space",
                Description = "Test Description"
            };
            
            var tableEntity = new TableEntity("test-id", "test-id");
            tableEntity["Name"] = "Test Address Space";
            tableEntity["Description"] = "Test Description";
            tableEntity["CreatedOn"] = DateTimeOffset.UtcNow;
            tableEntity["ModifiedOn"] = DateTimeOffset.UtcNow;
            tableEntity["PartitionKey"] = "test-id";
            
            mockTableServiceClient.Setup(client => client.GetTableClient("AddressSpaces"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.GetEntityAsync<TableEntity>("test-id", "test-id", null))
                .ReturnsAsync(Response.FromValue(tableEntity, new Mock<Response>().Object));

            // Act
            var result = await dataAccessService.GetAddressSpaceAsync("test-id");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpace.Id, result.Id);
            Assert.Equal(addressSpace.Name, result.Name);
            mockTableServiceClient.Verify(client => client.GetTableClient("AddressSpaces"), Times.Once);
            mockTableClient.Verify(client => client.GetEntityAsync<TableEntity>("test-id", "test-id", null), Times.Once);
        }
        
        [Fact]
        public async Task GetAddressSpacesAsync_ReturnsAllAddressSpaces()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var tableEntities = new List<TableEntity>
            {
                new TableEntity("1", "1")
                {
                    ["Name"] = "Address Space 1",
                    ["Description"] = "First address space",
                    ["CreatedOn"] = DateTimeOffset.UtcNow,
                    ["ModifiedOn"] = DateTimeOffset.UtcNow,
                    ["PartitionKey"] = "1"
                },
                new TableEntity("2", "2")
                {
                    ["Name"] = "Address Space 2",
                    ["Description"] = "Second address space",
                    ["CreatedOn"] = DateTimeOffset.UtcNow,
                    ["ModifiedOn"] = DateTimeOffset.UtcNow,
                    ["PartitionKey"] = "2"
                }
            };
            
            var page = Page<TableEntity>.FromValues(tableEntities, null, new Mock<Response>().Object);
            var pages = new List<Page<TableEntity>> { page };
            var mockAsyncPageable = new Mock<AsyncPageable<TableEntity>>();
            
            mockTableServiceClient.Setup(client => client.GetTableClient("AddressSpaces"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, null))
                .Returns(mockAsyncPageable.Object);
            
            // Setup the mock to return our pages
            mockAsyncPageable.Setup(p => p.AsPages(It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(pages);

            // Act
            var result = await dataAccessService.GetAddressSpacesAsync();

            // Assert
            Assert.NotNull(result);
            var addressSpaces = result.ToList();
            Assert.Equal(2, addressSpaces.Count);
            Assert.Equal("Address Space 1", addressSpaces[0].Name);
            Assert.Equal("Address Space 2", addressSpaces[1].Name);
            mockTableServiceClient.Verify(client => client.GetTableClient("AddressSpaces"), Times.Once);
            mockTableClient.Verify(client => client.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, null), Times.Once);
        }
        
        [Fact]
        public async Task CreateTagAsync_WithValidTag_ReturnsCreatedTag()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var tag = new Tag
            {
                Name = "Environment",
                Description = "Environment tag",
                Type = TagType.Inheritable,
                KnownValues = new List<string> { "Production", "Development" }
            };
            
            mockTableServiceClient.Setup(client => client.GetTableClient("Tags"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.AddEntityAsync(It.IsAny<TableEntity>()))
                .ReturnsAsync(new Response());

            // Act
            var result = await dataAccessService.CreateTagAsync("test-address-space", tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tag.Name, result.Name);
            Assert.Equal(tag.Type, result.Type);
            mockTableServiceClient.Verify(client => client.GetTableClient("Tags"), Times.Once);
            mockTableClient.Verify(client => client.AddEntityAsync(It.IsAny<TableEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task GetTagAsync_WithExistingTag_ReturnsTag()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var tag = new Tag
            {
                Name = "Environment",
                Description = "Environment tag",
                Type = TagType.Inheritable,
                KnownValues = new List<string> { "Production", "Development" }
            };
            
            var tableEntity = new TableEntity("test-address-space", "Environment");
            tableEntity["Description"] = "Environment tag";
            tableEntity["Type"] = "Inheritable";
            tableEntity["KnownValues"] = "[\"Production\",\"Development\"]";
            tableEntity["CreatedOn"] = DateTimeOffset.UtcNow;
            tableEntity["ModifiedOn"] = DateTimeOffset.UtcNow;
            
            mockTableServiceClient.Setup(client => client.GetTableClient("Tags"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.GetEntityAsync<TableEntity>("test-address-space", "Environment", null))
                .ReturnsAsync(Response.FromValue(tableEntity, new Mock<Response>().Object));

            // Act
            var result = await dataAccessService.GetTagAsync("test-address-space", "Environment");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tag.Name, result.Name);
            Assert.Equal(tag.Type, result.Type);
            mockTableServiceClient.Verify(client => client.GetTableClient("Tags"), Times.Once);
            mockTableClient.Verify(client => client.GetEntityAsync<TableEntity>("test-address-space", "Environment", null), Times.Once);
        }
        
        [Fact]
        public async Task GetTagsAsync_ReturnsAllTags()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var tableEntities = new List<TableEntity>
            {
                new TableEntity("test-address-space", "Environment")
                {
                    ["Description"] = "Environment tag",
                    ["Type"] = "Inheritable",
                    ["KnownValues"] = "[\"Production\",\"Development\"]",
                    ["CreatedOn"] = DateTimeOffset.UtcNow,
                    ["ModifiedOn"] = DateTimeOffset.UtcNow
                },
                new TableEntity("test-address-space", "Owner")
                {
                    ["Description"] = "Owner tag",
                    ["Type"] = "NonInheritable",
                    ["KnownValues"] = "[]",
                    ["CreatedOn"] = DateTimeOffset.UtcNow,
                    ["ModifiedOn"] = DateTimeOffset.UtcNow,
                    ["PartitionKey"] = "test-address-space"
                }
            };
            var page = Page<TableEntity>.FromValues(tableEntities, null, new Mock<Response>().Object);
            var pages = new List<Page<TableEntity>> { page };
            var mockAsyncPageable = new Mock<AsyncPageable<TableEntity>>();
            
            mockTableServiceClient.Setup(client => client.GetTableClient("Tags"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, null))
                .Returns(mockAsyncPageable.Object);
            
            // Setup the mock to return our pages
            mockAsyncPageable.Setup(p => p.AsPages(It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(pages);

            // Act
            var result = await dataAccessService.GetTagsAsync("test-address-space");

            // Assert
            Assert.NotNull(result);
            var tags = result.ToList();
            Assert.Equal(2, tags.Count);
            Assert.Equal("Environment", tags[0].Name);
            Assert.Equal("Owner", tags[1].Name);
            mockTableServiceClient.Verify(client => client.GetTableClient("Tags"), Times.Once);
            mockTableClient.Verify(client => client.QueryAsync<TableEntity>(It.IsAny<string>(), null, null, null), Times.Once);


            Assert.Equal("Production", returnValue[0].KnownValues.First());
            mockDataAccessService.Verify(service => service.GetIPAddressesAsync("default", null, tags), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddresses_WithInvalidAddressSpace_ReturnsNotFound()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IPAddressesController(mockDataAccessService.Object);
            
            mockDataAccessService.Setup(service => service.GetIPAddressesAsync("invalid-space", null, null))
                .ReturnsAsync((List<IPAddress>)null);

            // Act
            var result = await controller.GetIPAddresses("invalid-space");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            mockDataAccessService.Verify(service => service.GetIPAddressesAsync("invalid-space", null, null), Times.Once);
        }

        [Fact]
        public async Task UpdateAddressSpaceAsync_WithValidAddressSpace_ReturnsUpdatedAddressSpace()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var addressSpace = new AddressSpace
            {
                Id = "test-id",
                Name = "Updated Address Space",
                Description = "Updated Description"
            };
            
            mockTableServiceClient.Setup(client => client.GetTableClient("AddressSpaces"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.UpdateEntityAsync(It.IsAny<TableEntity>(), ETag.All))
                .Returns(Task.CompletedTask);

            // Act
            var result = await dataAccessService.UpdateAddressSpaceAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpace.Id, result.Id);
            Assert.Equal(addressSpace.Name, result.Name);
            mockTableServiceClient.Verify(client => client.GetTableClient("AddressSpaces"), Times.Once);
            mockTableClient.Verify(client => client.UpdateEntityAsync(It.IsAny<TableEntity>(), ETag.All), Times.Once);
        }
        
        [Fact]
        public async Task DeleteAddressSpaceAsync_WithValidId_DeletesAddressSpace()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var addressSpaceId = "test-id";
            
            mockTableServiceClient.Setup(client => client.GetTableClient("AddressSpaces"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.DeleteEntityAsync(addressSpaceId, addressSpaceId, It.IsAny<ETag>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await dataAccessService.DeleteAddressSpaceAsync(addressSpaceId);

            // Assert
            mockTableServiceClient.Verify(client => client.GetTableClient("AddressSpaces"), Times.Once);
            mockTableClient.Verify(client => client.DeleteEntityAsync(addressSpaceId, addressSpaceId, It.IsAny<ETag>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task UpdateTagAsync_WithValidTag_ReturnsUpdatedTag()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var tag = new Tag
            {
                Name = "Environment",
                Description = "Updated environment tag",
                Type = TagType.Inheritable,
                KnownValues = new List<string> { "Production", "Development", "Testing" }
            };
            
            mockTableServiceClient.Setup(client => client.GetTableClient("Tags"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.UpdateEntityAsync(It.IsAny<TableEntity>(), ETag.All))
                .Returns(Task.CompletedTask);

            // Act
            var result = await dataAccessService.UpdateTagAsync("test-address-space", tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tag.Name, result.Name);
            Assert.Equal(tag.Type, result.Type);
            mockTableServiceClient.Verify(client => client.GetTableClient("Tags"), Times.Once);
            mockTableClient.Verify(client => client.UpdateEntityAsync(It.IsAny<TableEntity>(), ETag.All), Times.Once);
        }
        
        [Fact]
        public async Task DeleteTagAsync_WithValidId_DeletesTag()
        {
            // Arrange
            var mockTableServiceClient = new Mock<TableServiceClient>();
            var mockTableClient = new Mock<TableClient>();
            var dataAccessService = new DataAccessService(mockTableServiceClient.Object);
            
            var addressSpaceId = "test-address-space";
            var tagName = "Environment";
            
            mockTableServiceClient.Setup(client => client.GetTableClient("Tags"))
                .Returns(mockTableClient.Object);
                
            mockTableClient.Setup(client => client.DeleteEntityAsync(addressSpaceId, tagName, It.IsAny<ETag>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await dataAccessService.DeleteTagAsync(addressSpaceId, tagName);

            // Assert
            mockTableServiceClient.Verify(client => client.GetTableClient("Tags"), Times.Once);
            mockTableClient.Verify(client => client.DeleteEntityAsync(addressSpaceId, tagName, It.IsAny<ETag>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}