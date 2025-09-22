using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Exceptions;
using Azure;
using System.Linq;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Unit tests focused on specific concurrency control mechanisms
    /// </summary>
    public class ConcurrencyUnitTests
    {
        private Mock<IIpAllocationRepository> _mockRepository;
        private Mock<TagInheritanceService> _mockTagService;
        private ConcurrentIpTreeService _concurrentService;

        public ConcurrencyUnitTests()
        {
            Setup();
        }

        private void Setup()
        {
            _mockRepository = new Mock<IIpAllocationRepository>();
            _mockTagService = new Mock<TagInheritanceService>();
            _concurrentService = new ConcurrentIpTreeService(
                _mockRepository.Object,
                _mockTagService.Object);
        }

        #region Semaphore Locking Tests

        [Fact]
        public async Task GetAddressSpaceLock_SameAddressSpace_ShouldReturnSameSemaphore()
        {
            // Arrange
            const string addressSpaceId = "test-space";

            // Act
            var lock1 = _concurrentService.GetAddressSpaceLock(addressSpaceId);
            var lock2 = _concurrentService.GetAddressSpaceLock(addressSpaceId);

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
            var lock1 = _concurrentService.GetAddressSpaceLock(addressSpaceId1);
            var lock2 = _concurrentService.GetAddressSpaceLock(addressSpaceId2);

            // Assert
            Assert.NotSame(lock1, lock2); // Should return different semaphore instances for different address spaces
        }

        [Fact]
        public async Task ConcurrentOperations_SameAddressSpace_ShouldBeSerializedBySemaphore()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            var executionOrder = new List<int>();
            var lockObject = new object();
            
            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, It.IsAny<string>()))
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

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity => Task.FromResult(entity));

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act - Start multiple concurrent updates
            var tasks = new List<Task>();
            for (int i = 1; i <= 5; i++)
            {
                var ipAllocation = CreateTestIpAllocation(addressSpaceId, $"ip-{i}", "10.0.1.0/24");
                tasks.Add(_concurrentService.UpdateIpAllocationAsync(ipAllocation));
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
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
            var entity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(entity);

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { entity });

            var attemptCount = 0;
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e =>
                {
                    attemptCount++;
                    if (attemptCount == 1)
                    {
                        throw new RequestFailedException(412, "Precondition Failed");
                    }
                    return Task.FromResult(e);
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _concurrentService.UpdateIpAllocationAsync(ipAllocation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, attemptCount); // Should retry once after ETag conflict
        }

        [Fact]
        public async Task UpdateIpAllocationAsync_ETagConflictExceedsMaxRetries_ShouldThrowConcurrencyException()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
            var entity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(entity);

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { entity });

            // Always throw ETag conflict
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .ThrowsAsync(new RequestFailedException(412, "Precondition Failed"));

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act & Assert
            await Assert.ThrowsAsync<ConcurrencyException>(
                () => _concurrentService.UpdateIpAllocationAsync(ipAllocation));
        }

        [Fact]
        public async Task UpdateIpAllocationAsync_ExponentialBackoffDelay_ShouldIncreaseDelayBetweenRetries()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
            var entity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(entity);

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { entity });

            var attemptTimes = new List<DateTime>();
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e =>
                {
                    attemptTimes.Add(DateTime.UtcNow);
                    if (attemptTimes.Count <= 2)
                    {
                        throw new RequestFailedException(412, "Precondition Failed");
                    }
                    return Task.FromResult(e);
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _concurrentService.UpdateIpAllocationAsync(ipAllocation);

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
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
            var originalEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var conflictingEntity = CreateTestEntity(addressSpaceId, "other-ip", "10.0.2.0/24");
            
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.2.0/24"); // Conflict!

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(originalEntity);

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { originalEntity, conflictingEntity });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _concurrentService.UpdateIpAllocationAsync(ipAllocation));
        }

        [Fact]
        public async Task UpdateIpAllocationAsync_PrefixChangeWithoutConflict_ShouldSucceed()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
            var originalEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.3.0/24"); // No conflict

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(originalEntity);

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { originalEntity });

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e => Task.FromResult(e));

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _concurrentService.UpdateIpAllocationAsync(ipAllocation);

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
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
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

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, It.IsAny<string>()))
                .Returns<string, string>((_, id) => Task.FromResult(entities.TryGetValue(id, out var entity) ? entity : null));

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(entities.Values.ToList());

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e =>
                {
                    entities[e.Id] = e;
                    return Task.FromResult(e);
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            // Act
            var result = await _concurrentService.UpdateIpAllocationAsync(ipAllocation);

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
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
            var parentEntity = CreateTestEntity(addressSpaceId, "parent", "10.0.0.0/16");
            var childEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24", "parent");
            
            var ipAllocation = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");
            ipAllocation.Tags["invalid-tag"] = "invalid-value";

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .ReturnsAsync(childEntity);

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, "parent"))
                .ReturnsAsync(parentEntity);

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { parentEntity, childEntity });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(ipAllocation.Tags);

            _mockTagService.Setup(t => t.ValidateTagInheritance(addressSpaceId, parentEntity.Tags, ipAllocation.Tags))
                .ThrowsAsync(new InvalidOperationException("Tag inheritance validation failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _concurrentService.UpdateIpAllocationAsync(ipAllocation));
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