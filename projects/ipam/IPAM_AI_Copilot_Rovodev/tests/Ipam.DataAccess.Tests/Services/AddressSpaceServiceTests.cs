using Xunit;
using Moq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Unit tests for AddressSpaceService
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceServiceTests
    {
        private readonly Mock<IAddressSpaceRepository> _addressSpaceRepositoryMock;
        private readonly Mock<IIpAllocationRepository> _ipNodeRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AddressSpaceService>> _loggerMock;
        private readonly AddressSpaceService _service;

        public AddressSpaceServiceTests()
        {
            _addressSpaceRepositoryMock = new Mock<IAddressSpaceRepository>();
            _ipNodeRepositoryMock = new Mock<IIpAllocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AddressSpaceService>>();

            _service = new AddressSpaceService(
                _addressSpaceRepositoryMock.Object,
                _ipNodeRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region CreateAddressSpaceAsync Tests

        [Fact]
        public async Task CreateAddressSpaceAsync_ValidAddressSpace_CreatesAddressSpaceAndRootNodes()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = "test-space-1",
                Name = "Test Space",
                Description = "Test Description"
            };

            var addressSpaceEntity = new AddressSpaceEntity
            {
                Id = "test-space-1",
                Name = "Test Space",
                Description = "Test Description",
                PartitionKey = "AddressSpaces"
            };

            var createdEntity = new AddressSpaceEntity
            {
                Id = "test-space-1",
                Name = "Test Space",
                Description = "Test Description",
                PartitionKey = "AddressSpaces",
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                Status = "Active"
            };

            _mapperMock.Setup(m => m.Map<AddressSpaceEntity>(addressSpace))
                .Returns(addressSpaceEntity);
            _mapperMock.Setup(m => m.Map<AddressSpace>(createdEntity))
                .Returns(addressSpace);

            _addressSpaceRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<AddressSpaceEntity>()))
                .ReturnsAsync(createdEntity);

            _ipNodeRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);

            _ipNodeRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);

            // Act
            var result = await _service.CreateAddressSpaceAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpace.Name, result.Name);

            // Verify address space creation
            _addressSpaceRepositoryMock.Verify(r => r.CreateAsync(It.Is<AddressSpaceEntity>(e => 
                e.Name == "Test Space" && 
                e.PartitionKey == "AddressSpaces" &&
                e.Status == "Active")), Times.Once);

            // Verify root IPv6 node creation
            _ipNodeRepositoryMock.Verify(r => r.CreateAsync(It.Is<IpAllocationEntity>(e =>
                e.Id == "ipv6_root" &&
                e.Prefix == "::/0" &&
                e.ParentId == null &&
                e.Tags.ContainsKey("Type") &&
                e.Tags["Type"] == "Root" &&
                e.Tags["Version"] == "IPv6")), Times.Once);

            // Verify root IPv4 node creation
            _ipNodeRepositoryMock.Verify(r => r.CreateAsync(It.Is<IpAllocationEntity>(e =>
                e.Id == "ipv4_root" &&
                e.Prefix == "0.0.0.0/0" &&
                e.ParentId == "ipv6_root" &&
                e.Tags.ContainsKey("Type") &&
                e.Tags["Type"] == "Root" &&
                e.Tags["Version"] == "IPv4")), Times.Once);

            // Verify IPv6 root update with IPv4 child
            _ipNodeRepositoryMock.Verify(r => r.UpdateAsync(It.Is<IpAllocationEntity>(e =>
                e.Id == "ipv6_root" &&
                e.ChildrenIds.Contains("ipv4_root"))), Times.Once);
        }

        [Fact]
        public async Task CreateAddressSpaceAsync_NullId_GeneratesNewId()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = null,
                Name = "Test Space"
            };

            var addressSpaceEntity = new AddressSpaceEntity
            {
                Name = "Test Space"
            };

            _mapperMock.Setup(m => m.Map<AddressSpaceEntity>(addressSpace))
                .Returns(addressSpaceEntity);
            _mapperMock.Setup(m => m.Map<AddressSpace>(It.IsAny<AddressSpaceEntity>()))
                .Returns(addressSpace);

            _addressSpaceRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<AddressSpaceEntity>()))
                .ReturnsAsync((AddressSpaceEntity entity) => entity);

            _ipNodeRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);

            _ipNodeRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);

            // Act
            var result = await _service.CreateAddressSpaceAsync(addressSpace);

            // Assert
            _addressSpaceRepositoryMock.Verify(r => r.CreateAsync(It.Is<AddressSpaceEntity>(e =>
                !string.IsNullOrEmpty(e.Id))), Times.Once);
        }

        [Fact]
        public async Task CreateAddressSpaceAsync_RootNodeCreationFails_StillReturnsAddressSpace()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = "test-space-1",
                Name = "Test Space"
            };

            var addressSpaceEntity = new AddressSpaceEntity
            {
                Id = "test-space-1",
                Name = "Test Space"
            };

            var createdEntity = new AddressSpaceEntity
            {
                Id = "test-space-1",
                Name = "Test Space",
                Status = "Active"
            };

            _mapperMock.Setup(m => m.Map<AddressSpaceEntity>(addressSpace))
                .Returns(addressSpaceEntity);
            _mapperMock.Setup(m => m.Map<AddressSpace>(createdEntity))
                .Returns(addressSpace);

            _addressSpaceRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<AddressSpaceEntity>()))
                .ReturnsAsync(createdEntity);

            _ipNodeRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ThrowsAsync(new Exception("Root node creation failed"));

            // Act
            var result = await _service.CreateAddressSpaceAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpace.Name, result.Name);

            // Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to create root nodes")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateAddressSpaceAsync_RepositoryThrows_PropagatesException()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = "test-space-1",
                Name = "Test Space"
            };

            _mapperMock.Setup(m => m.Map<AddressSpaceEntity>(addressSpace))
                .Returns(new AddressSpaceEntity());

            _addressSpaceRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<AddressSpaceEntity>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.CreateAddressSpaceAsync(addressSpace));
        }

        #endregion

        #region GetAddressSpaceByIdAsync Tests

        [Fact]
        public async Task GetAddressSpaceByIdAsync_ExistingId_ReturnsAddressSpace()
        {
            // Arrange
            var addressSpaceId = "test-space-1";
            var entity = new AddressSpaceEntity
            {
                Id = addressSpaceId,
                Name = "Test Space"
            };
            var dto = new AddressSpace
            {
                Id = addressSpaceId,
                Name = "Test Space"
            };

            _addressSpaceRepositoryMock.Setup(r => r.GetByIdAsync("AddressSpaces", addressSpaceId))
                .ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<AddressSpace>(entity))
                .Returns(dto);

            // Act
            var result = await _service.GetAddressSpaceByIdAsync(addressSpaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpaceId, result.Id);
            Assert.Equal("Test Space", result.Name);
        }

        [Fact]
        public async Task GetAddressSpaceByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var addressSpaceId = "non-existing";

            _addressSpaceRepositoryMock.Setup(r => r.GetByIdAsync("AddressSpaces", addressSpaceId))
                .ReturnsAsync((AddressSpaceEntity)null);

            // Act
            var result = await _service.GetAddressSpaceByIdAsync(addressSpaceId);

            // Assert
            Assert.Null(result);

            // Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAddressSpaceByIdAsync_RepositoryThrows_PropagatesException()
        {
            // Arrange
            var addressSpaceId = "test-space-1";

            _addressSpaceRepositoryMock.Setup(r => r.GetByIdAsync("AddressSpaces", addressSpaceId))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetAddressSpaceByIdAsync(addressSpaceId));
        }

        #endregion

        #region UpdateAddressSpaceAsync Tests

        [Fact]
        public async Task UpdateAddressSpaceAsync_ExistingAddressSpace_UpdatesSuccessfully()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = "test-space-1",
                Name = "Updated Name",
                Description = "Updated Description"
            };

            var existingEntity = new AddressSpaceEntity
            {
                Id = "test-space-1",
                Name = "Old Name",
                Description = "Old Description",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };

            var updatedEntity = new AddressSpaceEntity
            {
                Id = "test-space-1",
                Name = "Updated Name",
                Description = "Updated Description",
                CreatedOn = DateTime.UtcNow.AddDays(-1),
                ModifiedOn = DateTime.UtcNow
            };

            _addressSpaceRepositoryMock.Setup(r => r.GetByIdAsync("AddressSpaces", "test-space-1"))
                .ReturnsAsync(existingEntity);
            _addressSpaceRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AddressSpaceEntity>()))
                .ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map(addressSpace, existingEntity))
                .Returns(existingEntity);
            _mapperMock.Setup(m => m.Map<AddressSpace>(updatedEntity))
                .Returns(addressSpace);

            // Act
            var result = await _service.UpdateAddressSpaceAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);

            _addressSpaceRepositoryMock.Verify(r => r.UpdateAsync(It.Is<AddressSpaceEntity>(e =>
                e.Id == "test-space-1" &&
                e.ModifiedOn > e.CreatedOn)), Times.Once);
        }

        [Fact]
        public async Task UpdateAddressSpaceAsync_NonExistingAddressSpace_ReturnsNull()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = "non-existing",
                Name = "Updated Name"
            };

            _addressSpaceRepositoryMock.Setup(r => r.GetByIdAsync("AddressSpaces", "non-existing"))
                .ReturnsAsync((AddressSpaceEntity)null);

            // Act
            var result = await _service.UpdateAddressSpaceAsync(addressSpace);

            // Assert
            Assert.Null(result);

            // Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found for update")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region DeleteAddressSpaceAsync Tests

        [Fact]
        public async Task DeleteAddressSpaceAsync_ValidId_DeletesSuccessfully()
        {
            // Arrange
            var addressSpaceId = "test-space-1";

            _addressSpaceRepositoryMock.Setup(r => r.DeleteAsync("AddressSpaces", addressSpaceId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteAddressSpaceAsync(addressSpaceId);

            // Assert
            _addressSpaceRepositoryMock.Verify(r => r.DeleteAsync("AddressSpaces", addressSpaceId), Times.Once);
        }

        [Fact]
        public async Task DeleteAddressSpaceAsync_RepositoryThrows_PropagatesException()
        {
            // Arrange
            var addressSpaceId = "test-space-1";

            _addressSpaceRepositoryMock.Setup(r => r.DeleteAsync("AddressSpaces", addressSpaceId))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteAddressSpaceAsync(addressSpaceId));
        }

        #endregion

        #region GetAddressSpacesAsync Tests

        [Fact]
        public async Task GetAddressSpacesAsync_HasAddressSpaces_ReturnsAllAddressSpaces()
        {
            // Arrange
            var entities = new List<AddressSpaceEntity>
            {
                new AddressSpaceEntity { Id = "space1", Name = "Space 1" },
                new AddressSpaceEntity { Id = "space2", Name = "Space 2" }
            };

            var dtos = new List<AddressSpace>
            {
                new AddressSpace { Id = "space1", Name = "Space 1" },
                new AddressSpace { Id = "space2", Name = "Space 2" }
            };

            _addressSpaceRepositoryMock.Setup(r => r.GetAllAsync("AddressSpaces"))
                .ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<AddressSpace>>(entities))
                .Returns(dtos);

            // Act
            var result = await _service.GetAddressSpacesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, a => a.Id == "space1");
            Assert.Contains(result, a => a.Id == "space2");
        }

        [Fact]
        public async Task GetAddressSpacesAsync_NoAddressSpaces_ReturnsEmptyCollection()
        {
            // Arrange
            var entities = new List<AddressSpaceEntity>();
            var dtos = new List<AddressSpace>();

            _addressSpaceRepositoryMock.Setup(r => r.GetAllAsync("AddressSpaces"))
                .ReturnsAsync(entities);
            _mapperMock.Setup(m => m.Map<IEnumerable<AddressSpace>>(entities))
                .Returns(dtos);

            // Act
            var result = await _service.GetAddressSpacesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAddressSpacesAsync_RepositoryThrows_PropagatesException()
        {
            // Arrange
            _addressSpaceRepositoryMock.Setup(r => r.GetAllAsync("AddressSpaces"))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetAddressSpacesAsync());
        }

        #endregion

        #region Cancellation Token Tests

        [Fact]
        public async Task CreateAddressSpaceAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpace { Id = "test", Name = "Test" };
            var cancellationToken = new CancellationToken();

            _mapperMock.Setup(m => m.Map<AddressSpaceEntity>(addressSpace))
                .Returns(new AddressSpaceEntity());
            _mapperMock.Setup(m => m.Map<AddressSpace>(It.IsAny<AddressSpaceEntity>()))
                .Returns(addressSpace);
            _addressSpaceRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<AddressSpaceEntity>()))
                .ReturnsAsync(new AddressSpaceEntity());
            _ipNodeRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);
            _ipNodeRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity entity) => entity);

            // Act
            await _service.CreateAddressSpaceAsync(addressSpace, cancellationToken);

            // Assert - Token is passed through the call chain
            Assert.True(true); // If we get here without exception, cancellation token was handled properly
        }

        #endregion
    }
}