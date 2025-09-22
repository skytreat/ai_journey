using Xunit;
using Moq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Ipam.DataAccess.Entities;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Tests for concurrent scenarios in IP tree management
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class ConcurrentIpTreeServiceTests
    {
        private readonly Mock<IIpAllocationRepository> _ipNodeRepositoryMock;
        private readonly Mock<TagInheritanceService> _tagInheritanceServiceMock;
        private readonly ConcurrentIpTreeService _service;

        public ConcurrentIpTreeServiceTests()
        {
            _ipNodeRepositoryMock = new Mock<IIpAllocationRepository>();
            _tagInheritanceServiceMock = new Mock<TagInheritanceService>(new Mock<ITagRepository>().Object);
            _service = new ConcurrentIpTreeService(
                _ipNodeRepositoryMock.Object,
                _tagInheritanceServiceMock.Object);
        }

        [Fact]
        public async Task CreateIpNodeAsync_ConcurrentCallsWithSameParent_BothSucceed()
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentNode = new IpAllocationEntity
            {
                Id = "parent-id",
                Prefix = "10.0.0.0/16",
                Tags = new Dictionary<string, string> { { "Environment", "Production" } }
            };

            var child1Cidr = "10.0.1.0/24";
            var child2Cidr = "10.0.2.0/24";
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };

            // Setup mocks for successful creation
            _ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity> { parentNode });

            _ipNodeRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, parentNode.Id))
                .ReturnsAsync(parentNode);

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            _ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            _ipNodeRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            // Act - Create two nodes concurrently
            var task1 = _service.CreateIpAllocationAsync(addressSpaceId, child1Cidr, tags);
            var task2 = _service.CreateIpAllocationAsync(addressSpaceId, child2Cidr, tags);

            var results = await Task.WhenAll(task1, task2);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.NotNull(results[0]);
            Assert.NotNull(results[1]);
            Assert.Equal(child1Cidr, results[0].Prefix);
            Assert.Equal(child2Cidr, results[1].Prefix);
        }

        [Fact]
        public async Task CreateIpNodeAsync_ConcurrentCallsWithSameCidr_OneSucceedsOneThrows()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.1.0/24";
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };

            var callCount = 0;
            _ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        // First call - no existing nodes
                        return Task.FromResult<IEnumerable<IpAllocationEntity>>(new List<IpAllocationEntity>());
                    }
                    else
                    {
                        // Second call - node already exists
                        return Task.FromResult<IEnumerable<IpAllocationEntity>>(new List<IpAllocationEntity>
                        {
                            new IpAllocationEntity { Id = "existing", Prefix = cidr }
                        });
                    }
                });

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            _ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            // Act & Assert
            var task1 = _service.CreateIpAllocationAsync(addressSpaceId, cidr, tags);
            var task2 = _service.CreateIpAllocationAsync(addressSpaceId, cidr, tags);

            var result1 = await task1;
            await Assert.ThrowsAsync<InvalidOperationException>(() => task2);

            Assert.NotNull(result1);
            Assert.Equal(cidr, result1.Prefix);
        }

        [Fact]
        public async Task CreateIpNodeAsync_ParentDeletedDuringCreation_ThrowsConcurrencyException()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.1.0/24";
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };
            var parentNode = new IpAllocationEntity
            {
                Id = "parent-id",
                Prefix = "10.0.0.0/16"
            };

            _ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity> { parentNode });

            // First call returns parent, second call returns null (parent deleted)
            var getByIdCallCount = 0;
            _ipNodeRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, parentNode.Id))
                .Returns(() =>
                {
                    getByIdCallCount++;
                    return getByIdCallCount == 1 
                        ? Task.FromResult(parentNode)
                        : Task.FromResult<IpAllocationEntity>(null);
                });

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            // Act & Assert
            await Assert.ThrowsAsync<ConcurrencyException>(
                () => _service.CreateIpAllocationAsync(addressSpaceId, cidr, tags));
        }

        [Fact]
        public async Task CreateIpNodeAsync_ETagConflictWithRetry_EventuallySucceeds()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.1.0/24";
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };

            _ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            // First call throws conflict, second succeeds
            var createCallCount = 0;
            _ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns((IpAllocationEntity node) =>
                {
                    createCallCount++;
                    if (createCallCount == 1)
                    {
                        throw new RequestFailedException(409, "Conflict");
                    }
                    return Task.FromResult(node);
                });

            // Act
            var result = await _service.CreateIpAllocationAsync(addressSpaceId, cidr, tags);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cidr, result.Prefix);
            Assert.Equal(2, createCallCount); // Should have retried once
        }

        [Fact]
        public async Task CreateIpNodeAsync_MaxRetriesExceeded_ThrowsConcurrencyException()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.1.0/24";
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };

            _ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            // Always throw conflict
            _ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ThrowsAsync(new RequestFailedException(409, "Conflict"));

            // Act & Assert
            await Assert.ThrowsAsync<ConcurrencyException>(
                () => _service.CreateIpAllocationAsync(addressSpaceId, cidr, tags));
        }

        [Fact]
        public async Task CreateIpNodeAsync_TagConflictBetweenParentChild_ThrowsInvalidOperationException()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.1.0/24";
            var parentNode = new IpAllocationEntity
            {
                Id = "parent-id",
                Prefix = "10.0.0.0/16",
                Tags = new Dictionary<string, string> { { "Environment", "Production" } }
            };
            var conflictingTags = new Dictionary<string, string> { { "Environment", "Development" } };

            _ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity> { parentNode });

            _ipNodeRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, parentNode.Id))
                .ReturnsAsync(parentNode);

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, conflictingTags))
                .ReturnsAsync(conflictingTags);

            _tagInheritanceServiceMock.Setup(x => x.ValidateTagInheritance(
                addressSpaceId, parentNode.Tags, conflictingTags))
                .ThrowsAsync(new InvalidOperationException("Tag conflict"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateIpAllocationAsync(addressSpaceId, cidr, conflictingTags));
        }

        [Fact]
        public async Task DeleteIpNodeAsync_ConcurrentDeletion_HandlesGracefully()
        {
            // Arrange
            var addressSpaceId = "space1";
            var ipId = "node-to-delete";
            var nodeToDelete = new IpAllocationEntity
            {
                Id = ipId,
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
                ParentId = "parent-id"
            };

            var deleteCallCount = 0;
            _ipNodeRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, ipId))
                .Returns(() =>
                {
                    deleteCallCount++;
                    return deleteCallCount == 1 
                        ? Task.FromResult(nodeToDelete)
                        : Task.FromResult<IpAllocationEntity>(null); // Second call - already deleted
                });

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, ipId))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipNodeRepositoryMock.Setup(x => x.DeleteAsync(addressSpaceId, ipId))
                .Returns(Task.CompletedTask);

            // Act - Delete concurrently
            var task1 = _service.DeleteIpAllocationAsync(addressSpaceId, ipId);
            var task2 = _service.DeleteIpAllocationAsync(addressSpaceId, ipId);

            await Task.WhenAll(task1, task2);

            // Assert - Should complete without throwing
            _ipNodeRepositoryMock.Verify(x => x.DeleteAsync(addressSpaceId, ipId), Times.Once);
        }

        [Fact]
        public async Task CreateIpNodeAsync_SameCidrAsParentWithoutAdditionalTags_ThrowsInvalidOperationException()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.0.0/16"; // Same as parent
            var parentNode = new IpAllocationEntity
            {
                Id = "parent-id",
                Prefix = cidr, // Same CIDR
                Tags = new Dictionary<string, string> { { "Environment", "Production" } }
            };
            var sameTags = new Dictionary<string, string> { { "Environment", "Production" } }; // Same tags

            var ipNodeRepositoryMock = new Mock<IIpAllocationRepository>();
            ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity> { parentNode });

            ipNodeRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, parentNode.Id))
                .ReturnsAsync(parentNode);

            // Mock tag repository to return inheritable tag
            var mockTagRepo = new Mock<ITagRepository>();
            mockTagRepo.Setup(x => x.GetByNameAsync(addressSpaceId, "Environment"))
                .ReturnsAsync(new TagEntity { Type = "Inheritable" });
            
            var tagInheritanceService = new TagInheritanceService(mockTagRepo.Object);

            var service = new ConcurrentIpTreeService(
                ipNodeRepositoryMock.Object,
                tagInheritanceService);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateIpAllocationAsync(addressSpaceId, cidr, sameTags));
        }

        [Fact]
        public async Task CreateIpNodeAsync_ConcurrentCreationInDifferentAddressSpaces_BothSucceed()
        {
            // Arrange
            var addressSpace1 = "space1";
            var addressSpace2 = "space2";
            var cidr = "10.0.1.0/24"; // Same CIDR in different address spaces
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };

            _ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(It.IsAny<string>(), cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(It.IsAny<string>(), tags))
                .ReturnsAsync(tags);

            _ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            // Act - Create in different address spaces concurrently
            var task1 = _service.CreateIpAllocationAsync(addressSpace1, cidr, tags);
            var task2 = _service.CreateIpAllocationAsync(addressSpace2, cidr, tags);

            var results = await Task.WhenAll(task1, task2);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.Equal(addressSpace1, results[0].AddressSpaceId);
            Assert.Equal(addressSpace2, results[1].AddressSpaceId);
            Assert.Equal(cidr, results[0].Prefix);
            Assert.Equal(cidr, results[1].Prefix);
        }

        [Fact]
        public async Task CreateIpNodeAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.1.0/24";
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };
            var cancellationToken = new CancellationToken(true); // Already cancelled

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _service.CreateIpAllocationAsync(addressSpaceId, cidr, tags, cancellationToken));
        }
    }
}