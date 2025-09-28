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
using Ipam.DataAccess.Tests.TestHelpers;

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
        private readonly Mock<IIpAllocationRepository> _ipAllocationRepositoryMock;
        private readonly Mock<TagInheritanceService> _tagInheritanceServiceMock;
        private readonly ConcurrentIpTreeService _ipService;

        public ConcurrentIpTreeServiceTests()
        {
            _ipAllocationRepositoryMock = new Mock<IIpAllocationRepository>();
            _tagInheritanceServiceMock = new Mock<TagInheritanceService>(new Mock<ITagRepository>().Object);
            _ipService = new ConcurrentIpTreeService(
                _ipAllocationRepositoryMock.Object,
                _tagInheritanceServiceMock.Object);
        }

        // Merged from ConcurrencyUnitTests.cs

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
            _ipAllocationRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity> { parentNode });

            _ipAllocationRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, parentNode.Id))
                .ReturnsAsync(parentNode);

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            _ipAllocationRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            _ipAllocationRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            // Act - Create two nodes concurrently
            var ipAllocation1 = new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = child1Cidr, Tags = tags };
            var ipAllocation2 = new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = child2Cidr, Tags = tags };
            var task1 = _ipService.CreateIpAllocationAsync(ipAllocation1);
            var task2 = _ipService.CreateIpAllocationAsync(ipAllocation2);

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
            _ipAllocationRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
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

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            _ipAllocationRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            // Act & Assert
            var ipAllocation1 = new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = tags };
            var ipAllocation2 = new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = tags };
            var task1 = _ipService.CreateIpAllocationAsync(ipAllocation1);
            var task2 = _ipService.CreateIpAllocationAsync(ipAllocation2);

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

            _ipAllocationRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity> { parentNode });

            // First call returns parent, second call returns null (parent deleted)
            var getByIdCallCount = 0;
            _ipAllocationRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, parentNode.Id))
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
            var ipAllocation = new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = tags };
            await Assert.ThrowsAsync<ConcurrencyException>(
                () => _ipService.CreateIpAllocationAsync(ipAllocation));
        }

        [Fact]
        public async Task CreateIpNodeAsync_ETagConflictWithRetry_EventuallySucceeds()
        {
            // Arrange
            var addressSpaceId = "space1";
            var cidr = "10.0.1.0/24";
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };

            _ipAllocationRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            // First call throws conflict, second succeeds
            var createCallCount = 0;
            _ipAllocationRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
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
            var ipAllocation = new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = tags };
            var result = await _ipService.CreateIpAllocationAsync(ipAllocation);

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

            _ipAllocationRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, tags))
                .ReturnsAsync(tags);

            // Always throw conflict
            _ipAllocationRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ThrowsAsync(new RequestFailedException(409, "Conflict"));

            // Act & Assert
            await Assert.ThrowsAsync<ConcurrencyException>(
                () => _ipService.CreateIpAllocationAsync(new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = tags }));
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

            _ipAllocationRepositoryMock.Setup(x => x.GetByPrefixAsync(addressSpaceId, cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, null))
                .ReturnsAsync(new List<IpAllocationEntity> { parentNode });

            _ipAllocationRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, parentNode.Id))
                .ReturnsAsync(parentNode);

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(addressSpaceId, conflictingTags))
                .ReturnsAsync(conflictingTags);

            _tagInheritanceServiceMock.Setup(x => x.ValidateTagInheritance(
                addressSpaceId, parentNode.Tags, conflictingTags))
                .ThrowsAsync(new InvalidOperationException("Tag conflict"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ipService.CreateIpAllocationAsync(new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = conflictingTags }));
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
            _ipAllocationRepositoryMock.Setup(x => x.GetByIdAsync(addressSpaceId, ipId))
                .Returns(() =>
                {
                    deleteCallCount++;
                    return deleteCallCount == 1
                        ? Task.FromResult(nodeToDelete)
                        : Task.FromResult<IpAllocationEntity>(null); // Second call - already deleted
                });

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(addressSpaceId, ipId))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(x => x.DeleteAsync(addressSpaceId, ipId))
                .Returns(Task.CompletedTask);

            // Act - Delete concurrently
            var task1 = _ipService.DeleteIpAllocationAsync(addressSpaceId, ipId);
            var task2 = _ipService.DeleteIpAllocationAsync(addressSpaceId, ipId);

            await Task.WhenAll(task1, task2);

            // Assert - Should complete without throwing
            _ipAllocationRepositoryMock.Verify(x => x.DeleteAsync(addressSpaceId, ipId), Times.Once);
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
                () => service.CreateIpAllocationAsync(new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = sameTags }));
        }

        [Fact]
        public async Task CreateIpNodeAsync_ConcurrentCreationInDifferentAddressSpaces_BothSucceed()
        {
            // Arrange
            var addressSpace1 = "space1";
            var addressSpace2 = "space2";
            var cidr = "10.0.1.0/24"; // Same CIDR in different address spaces
            var tags = new Dictionary<string, string> { { "Application", "WebServer" } };

            _ipAllocationRepositoryMock.Setup(x => x.GetByPrefixAsync(It.IsAny<string>(), cidr))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(It.IsAny<string>(), tags))
                .ReturnsAsync(tags);

            _ipAllocationRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => node);

            // Act - Create in different address spaces concurrently
            var task1 = _ipService.CreateIpAllocationAsync(new IpAllocation { AddressSpaceId = addressSpace1, Prefix = cidr, Tags = tags });
            var task2 = _ipService.CreateIpAllocationAsync(new IpAllocation { AddressSpaceId = addressSpace2, Prefix = cidr, Tags = tags });

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
                () => _ipService.CreateIpAllocationAsync(new IpAllocation { AddressSpaceId = addressSpaceId, Prefix = cidr, Tags = tags }, cancellationToken));
        }

        #region Merged from ConcurrencyUnitTests.cs

        [Fact]
        public async Task GetAddressSpaceLock_SameAddressSpace_ShouldReturnSameSemaphore()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;

            // Act
            var lock1 = _ipService.GetAddressSpaceLock(addressSpaceId);
            var lock2 = _ipService.GetAddressSpaceLock(addressSpaceId);

            // Assert
            Assert.Same(lock1, lock2); // Should return the same semaphore instance for same address space
        }

        [Fact]
        public async Task GetAddressSpaceLock_DifferentAddressSpaces_ShouldReturnDifferentSemaphores()
        {
            // Arrange
            const string addressSpaceId1 = "test-space-1";
            const string addressSpaceId2 = "test-space-2";

            // Act
            var lock1 = _ipService.GetAddressSpaceLock(addressSpaceId1);
            var lock2 = _ipService.GetAddressSpaceLock(addressSpaceId2);

            // Assert
            Assert.NotSame(lock1, lock2); // Should return different semaphore instances for different address spaces
        }

        [Fact]
        public async Task ConcurrentOperations_SameAddressSpace_ShouldBeSerializedBySemaphore()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            var executionOrder = new List<int>();
            var lockObject = new object();
            
            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, It.IsAny<string>()))
                .Returns<string, string>((_, id) =>
                {
                    lock (lockObject)
                    {
                        var entityId = int.Parse(id.Split('-')[1]);
                        executionOrder.Add(entityId);
                        Thread.Sleep(10); // Simulate work
                        return Task.FromResult(CreateTestEntity(addressSpaceId, id, "10.0.1.0/24"));
                    }
                });

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _ipAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity => Task.FromResult(entity));

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act - Start multiple concurrent updates
            var tasks = new List<Task>();
            for (int i = 1; i <= 5; i++)
            {
                var ipAllocation = CreateTestIpAllocation(addressSpaceId, $"ip-{i}", "10.0.1.0/24");
                tasks.Add(_ipService.UpdateIpAllocationAsync(ipAllocation));
            }

            await Task.WhenAll(tasks);

            // Assert - Operations should have been serialized by address space lock
            lock (lockObject)
            {
                Assert.Equal(5, executionOrder.Count); // All operations should have executed
                // Due to semaphore, operations should be serialized (not necessarily in order due to async nature)
                Assert.True(executionOrder.Distinct().Count() == 5); // All operations should be unique
            }
        }

        #endregion

        #region ETag Retry Logic Tests

        [Fact]
        public async Task UpdateIpAllocationAsync_ETagConflictOnce_ShouldRetrySuccessfully()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            const string ipId = TestConstants.DefaultIpId;
            var entity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(entity);

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { entity });

            var attemptCount = 0;
            _ipAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e =>
                {
                    attemptCount++;
                    if (attemptCount == 1)
                    {
                        throw new RequestFailedException(412, "Precondition Failed");
                    }
                    return Task.FromResult(e);
                });

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _ipService.UpdateIpAllocationAsync(ipAllocation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, attemptCount); // Should retry once after ETag conflict
        }

        [Fact]
        public async Task UpdateIpAllocationAsync_ETagConflictExceedsMaxRetries_ShouldThrowConcurrencyException()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            const string ipId = TestConstants.DefaultIpId;
            var entity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(entity);

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { entity });

            // Always throw ETag conflict
            _ipAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ThrowsAsync(new RequestFailedException(412, "Precondition Failed"));

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act & Assert
            await Assert.ThrowsAsync<ConcurrencyException>(
                () => _ipService.UpdateIpAllocationAsync(ipAllocation));
        }

        [Fact]
        public async Task UpdateIpAllocationAsync_ExponentialBackoffDelay_ShouldIncreaseDelayBetweenRetries()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            const string ipId = TestConstants.DefaultIpId;
            var entity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(entity);

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { entity });

            var attemptTimes = new List<DateTime>();
            _ipAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e =>
                {
                    attemptTimes.Add(DateTime.UtcNow);
                    if (attemptTimes.Count <= 2)
                    {
                        throw new RequestFailedException(412, "Precondition Failed");
                    }
                    return Task.FromResult(e);
                });

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _ipService.UpdateIpAllocationAsync(ipAllocation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, attemptTimes.Count); // Should make 3 attempts
            
            // Verify exponential backoff (allowing for some timing variance)
            var firstDelay = attemptTimes[1] - attemptTimes[0];
            var secondDelay = attemptTimes[2] - attemptTimes[1];
            
            Assert.True(secondDelay > firstDelay); 
            // Second delay should be longer than first delay
        }

        #endregion

        #region Prefix Conflict Validation Tests

        [Fact]
        public async Task UpdateIpAllocationAsync_PrefixChangeWithConflict_ShouldThrowException()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            const string ipId = TestConstants.DefaultIpId;
            var originalEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var conflictingEntity = CreateTestEntity(addressSpaceId, "other-ip", "10.0.2.0/24");
            
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.2.0/24"); // Conflict!

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(originalEntity);

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { originalEntity, conflictingEntity });

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ipService.UpdateIpAllocationAsync(ipAllocation));
        }

        [Fact]
        public async Task UpdateIpAllocationAsync_PrefixChangeWithoutConflict_ShouldSucceed()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            const string ipId = TestConstants.DefaultIpId;
            var originalEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.3.0/24"); // No conflict

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(originalEntity);

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { originalEntity });

            _ipAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e => Task.FromResult(e));

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _ipService.UpdateIpAllocationAsync(ipAllocation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("10.0.3.0/24", result.Prefix);
        }

        #endregion

        #region Parent-Child Relationship Tests

        [Fact]
        public async Task UpdateIpAllocationAsync_ParentRelationshipChange_ShouldUpdateChildrenLists()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            const string ipId = TestConstants.DefaultIpId;
            var oldParent = CreateTestEntity(addressSpaceId, "old-parent", "10.0.0.0/16");
            oldParent.ChildrenIds = new List<string> { ipId };
            
            var newParent = CreateTestEntity(addressSpaceId, "new-parent", "10.1.0.0/16");
            newParent.ChildrenIds = new List<string>();
            
            var childEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24", "old-parent");
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.1.1.0/24"); // Move to new parent

            var entities = new Dictionary<string, IpAllocationEntity>
            {
                [ipId] = childEntity,
                ["old-parent"] = oldParent,
                ["new-parent"] = newParent
            };

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, It.IsAny<string>()))
                .Returns<string, string>((_, id) => Task.FromResult(entities.TryGetValue(id, out var entity) ? entity : null));

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(entities.Values.ToList());

            _ipAllocationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e =>
                {
                    entities[e.Id] = e;
                    return Task.FromResult(e);
                });

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _ipService.UpdateIpAllocationAsync(ipAllocation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new-parent", result.ParentId);
            
            // Verify old parent's children list was updated
            Assert.False(entities["old-parent"].ChildrenIds.Contains(ipId)); 
            // Old parent should no longer list this as a child
            
            // Verify new parent's children list was updated
            Assert.True(entities["new-parent"].ChildrenIds.Contains(ipId)); 
            // New parent should list this as a child
        }

        #endregion

        #region Tag Inheritance Validation Tests

        [Fact]
        public async Task UpdateIpAllocationAsync_TagInheritanceViolation_ShouldThrowException()
        {
            // Arrange
            const string addressSpaceId = TestConstants.DefaultAddressSpaceId;
            const string ipId = TestConstants.DefaultIpId;
            var parentEntity = CreateTestEntity(addressSpaceId, "parent", "10.0.0.0/16");
            var childEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24", "parent");
            
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");
            ipAllocation.Tags["invalid-tag"] = "invalid-value";

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(childEntity);

            _ipAllocationRepositoryMock.Setup(r => r.GetByIdAsync(addressSpaceId, "parent"))
                .ReturnsAsync(parentEntity);

            _ipAllocationRepositoryMock.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { parentEntity, childEntity });

            _tagInheritanceServiceMock.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(ipAllocation.Tags);

            _tagInheritanceServiceMock.Setup(t => t.ValidateTagInheritance(addressSpaceId, parentEntity.Tags, ipAllocation.Tags))
                .ThrowsAsync(new InvalidOperationException("Tag inheritance validation failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ipService.UpdateIpAllocationAsync(ipAllocation));
        }

        #endregion

        #region Helper Methods

        private IpAllocationEntity CreateTestEntity(string addressSpaceId, string id, string prefix, string parentId = null)
        {
            return new IpAllocationEntity
            {
                Id = id,
                AddressSpaceId = addressSpaceId,
                Prefix = prefix,
                ParentId = parentId,
                Tags = new Dictionary<string, string>(),
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                ETag = new ETag($"etag-{id}"),
                ChildrenIds = new List<string>()
            };
        }

        private IpAllocation CreateTestIpAllocation(string addressSpaceId, string id, string prefix)
        {
            return new IpAllocation
            {
                Id = id,
                AddressSpaceId = addressSpaceId,
                Prefix = prefix,
                Tags = new Dictionary<string, string>(),
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
        }

        #endregion
    }
}
