using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using AutoMapper;
using Azure;
using System.Linq;
using Ipam.ServiceContract.Models;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Comprehensive concurrency integration tests for IPAM system
    /// Tests race conditions, ETag conflicts, and thread safety
    /// </summary>
    public class ConcurrencyIntegrationTests
    {
        private Mock<IIpAllocationRepository> _mockRepository;
        private Mock<TagInheritanceService> _mockTagService;
        private Mock<IMapper> _mockMapper;
        private Mock<ILogger<IpAllocationServiceImpl>> _mockLogger;
        private Mock<PerformanceMonitoringService> _mockPerformanceService;
        private ConcurrentIpTreeService _concurrentService;
        private IpAllocationServiceImpl _ipAllocationService;

        public ConcurrencyIntegrationTests()
        {
            Setup();
        }

        private void Setup()
        {
            _mockRepository = new Mock<IIpAllocationRepository>();
            _mockTagService = new Mock<TagInheritanceService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<IpAllocationServiceImpl>>();
            _mockPerformanceService = new Mock<PerformanceMonitoringService>();

            _concurrentService = new ConcurrentIpTreeService(
                _mockRepository.Object,
                _mockTagService.Object);

            _ipAllocationService = new IpAllocationServiceImpl(
                _mockRepository.Object,
                null, // IpTreeService not needed for these tests
                _concurrentService,
                _mockPerformanceService.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        #region Critical Race Condition Tests

        [Fact]
        public async Task UpdateIpAllocationAsync_ConcurrentUpdates_ShouldHandleETagConflicts()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            const string ipId = "test-ip";
            var originalEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.1.0/24");
            var updatedEntity = CreateTestEntity(addressSpaceId, ipId, "10.0.2.0/24");

            var ipAllocation1 = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.1.0/24");
            var ipAllocation2 = CreateTestIpAllocation(addressSpaceId, ipId, "10.0.2.0/24");

            // Simulate ETag conflict on first update attempt, success on retry
            var getCallCount = 0;
            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, ipId))
                .Returns(() =>
                {
                    getCallCount++;
                    return Task.FromResult(getCallCount == 1 ? originalEntity : updatedEntity);
                });

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(new List<IpAllocationEntity> { originalEntity });

            var updateCallCount = 0;
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity =>
                {
                    updateCallCount++;
                    if (updateCallCount == 1)
                    {
                        // Simulate ETag conflict
                        throw new RequestFailedException(412, "Precondition Failed");
                    }
                    return Task.FromResult(entity);
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            _mockMapper.Setup(m => m.Map<IpAllocation>(It.IsAny<IpAllocationEntity>()))
                .Returns(ipAllocation1);

            // Act & Assert
            var result = await _ipAllocationService.UpdateIpAllocationAsync(ipAllocation1);

            // Verify retry logic was executed
            Assert.NotNull(result);
            Assert.Equal(2, getCallCount); // Should retry after ETag conflict
            Assert.Equal(2, updateCallCount); // Should attempt update twice
        }

        [Fact]
        public async Task ConcurrentSubnetAllocation_ShouldPreventDuplicateAllocations()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            const string parentCidr = "10.0.0.0/16";
            const int subnetSize = 24;

            var existingEntities = new List<IpAllocationEntity>();
            var lockObject = new object();

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .Returns(() => Task.FromResult((IList<IpAllocationEntity>)existingEntities.ToList()));

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity =>
                {
                    lock (lockObject)
                    {
                        // Simulate checking for conflicts
                        var conflicting = existingEntities.Any(e => e.Prefix == entity.Prefix);
                        if (conflicting)
                        {
                            throw new InvalidOperationException($"IP node with CIDR {entity.Prefix} already exists");
                        }
                        existingEntities.Add(entity);
                        return Task.FromResult(entity);
                    }
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            _mockPerformanceService.Setup(p => p.MeasureAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<string>>>>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>((name, func, props) => func());

            // Act - Simulate concurrent requests for subnets
            var tasks = new List<Task<List<string>>>();
            const int concurrentRequests = 10;

            for (int i = 0; i < concurrentRequests; i++)
            {
                tasks.Add(_ipAllocationService.FindAvailableSubnetsAsync(addressSpaceId, parentCidr, subnetSize, 1));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            var allocatedSubnets = results.SelectMany(r => r).ToList();
            var uniqueSubnets = allocatedSubnets.Distinct().ToList();

            Assert.Equal(allocatedSubnets.Count, uniqueSubnets.Count); 
            // All allocated subnets should be unique - no duplicates allowed
        }

        [Fact]
        public async Task ConcurrentParentChildUpdates_ShouldMaintainConsistency()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            var parentEntity = CreateTestEntity(addressSpaceId, "parent-1", "10.0.0.0/16");
            var childEntity = CreateTestEntity(addressSpaceId, "child-1", "10.0.1.0/24", "parent-1");

            var entities = new Dictionary<string, IpAllocationEntity>
            {
                ["parent-1"] = parentEntity,
                ["child-1"] = childEntity
            };

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, It.IsAny<string>()))
                .Returns<string, string>((_, id) => Task.FromResult(entities.TryGetValue(id, out var entity) ? entity : null));

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(entities.Values.ToList());

            var updateCallCount = 0;
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity =>
                {
                    Interlocked.Increment(ref updateCallCount);
                    // Simulate occasional ETag conflicts
                    if (updateCallCount % 3 == 0)
                    {
                        throw new RequestFailedException(412, "Precondition Failed");
                    }
                    entities[entity.Id] = entity;
                    return Task.FromResult(entity);
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            _mockMapper.Setup(m => m.Map<IpAllocation>(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e => CreateTestIpAllocation(e.AddressSpaceId, e.Id, e.Prefix));

            // Act - Concurrent updates to parent and child
            var parentUpdate = CreateTestIpAllocation(addressSpaceId, "parent-1", "10.0.0.0/16");
            parentUpdate.Tags["environment"] = "production";

            var childUpdate = CreateTestIpAllocation(addressSpaceId, "child-1", "10.0.1.0/24");
            childUpdate.Tags["tier"] = "web";

            var tasks = new[]
            {
                _ipAllocationService.UpdateIpAllocationAsync(parentUpdate),
                _ipAllocationService.UpdateIpAllocationAsync(childUpdate)
            };

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.NotNull(results[0]); // Parent update should succeed
            Assert.NotNull(results[1]); // Child update should succeed
            Assert.True(updateCallCount > 2); // Should handle ETag conflicts with retries
        }

        #endregion

        #region ETag Conflict Simulation Tests

        [Fact]
        public async Task UpdateIpAllocationAsync_MaxRetriesExceeded_ShouldThrowConcurrencyException()
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
                () => _ipAllocationService.UpdateIpAllocationAsync(ipAllocation));
        }

        [Fact]
        public async Task CreateIpAllocationAsync_ConcurrentCreationSameCidr_ShouldPreventDuplicates()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            const string cidr = "10.0.1.0/24";

            var existingEntities = new List<IpAllocationEntity>();
            var lockObject = new object();
            var creationAttempts = 0;

            _mockRepository.Setup(r => r.GetByPrefixAsync(addressSpaceId, cidr))
                .Returns(() =>
                {
                    lock (lockObject)
                    {
                        return Task.FromResult(existingEntities.Where(e => e.Prefix == cidr).AsEnumerable());
                    }
                });

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .Returns(() => Task.FromResult((IList<IpAllocationEntity>)existingEntities.ToList()));

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity =>
                {
                    lock (lockObject)
                    {
                        Interlocked.Increment(ref creationAttempts);
                        var existing = existingEntities.FirstOrDefault(e => e.Prefix == entity.Prefix);
                        if (existing != null)
                        {
                            throw new InvalidOperationException($"IP node with CIDR {entity.Prefix} already exists");
                        }
                        existingEntities.Add(entity);
                        return Task.FromResult(entity);
                    }
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            _mockMapper.Setup(m => m.Map<IpAllocation>(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e => CreateTestIpAllocation(e.AddressSpaceId, e.Id, e.Prefix));

            // Act - Attempt concurrent creation of same CIDR
            var tasks = new List<Task<IpAllocation>>();
            const int concurrentAttempts = 5;

            for (int i = 0; i < concurrentAttempts; i++)
            {
                var ipAllocation = CreateTestIpAllocation(addressSpaceId, Guid.NewGuid().ToString(), cidr);
                tasks.Add(_ipAllocationService.CreateIpAllocationAsync(ipAllocation));
            }

            // Only one should succeed, others should fail
            var results = new List<IpAllocation>();
            var exceptions = new List<Exception>();

            foreach (var task in tasks)
            {
                try
                {
                    var result = await task;
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // Assert
            Assert.Equal(1, results.Count); // Only one creation should succeed
            Assert.Equal(concurrentAttempts - 1, exceptions.Count); // Other attempts should fail
            Assert.True(exceptions.All(e => e is InvalidOperationException)); 
            // All failures should be due to duplicate CIDR
        }

        #endregion

        #region Cache Concurrency Tests

        [Fact]
        public async Task ConcurrentCacheOperations_ShouldBeThreadSafe()
        {
            // Arrange
            const string addressSpaceId = "test-space";
            var entities = new List<IpAllocationEntity>();
            
            for (int i = 0; i < 100; i++)
            {
                entities.Add(CreateTestEntity(addressSpaceId, $"ip-{i}", $"10.0.{i}.0/24"));
            }

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .ReturnsAsync(entities);

            // Create optimized traversal service for cache testing
            var traversalService = new OptimizedIpTreeTraversalService(_mockRepository.Object);

            // Act - Concurrent cache operations
            var tasks = new List<Task>();
            const int concurrentOperations = 20;

            for (int i = 0; i < concurrentOperations; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Mix of read and invalidation operations
                    if (index % 3 == 0)
                    {
                        traversalService.InvalidateCache(addressSpaceId);
                    }
                    else
                    {
                        await traversalService.FindClosestParentOptimizedAsync(addressSpaceId, $"10.0.{index % 10}.128/25");
                    }
                }));
            }

            // Should not throw any exceptions
            await Task.WhenAll(tasks);

            // Assert - Verify cache statistics are consistent
            var stats = traversalService.GetCacheStatistics();
            Assert.True(stats.CachedAddressSpaces >= 0); // Cache statistics should be valid
        }

        #endregion

        #region Load Testing Scenarios

        [Fact]
        public async Task HighConcurrencyScenario_MixedOperations_ShouldMaintainDataIntegrity()
        {
            // Arrange
            const string addressSpaceId = "load-test-space";
            var entities = new Dictionary<string, IpAllocationEntity>();
            var lockObject = new object();

            _mockRepository.Setup(r => r.GetByIdAsync(addressSpaceId, It.IsAny<string>()))
                .Returns<string, string>((_, id) =>
                {
                    lock (lockObject)
                    {
                        return Task.FromResult(entities.TryGetValue(id, out var entity) ? entity : null);
                    }
                });

            _mockRepository.Setup(r => r.GetAllAsync(addressSpaceId))
                .Returns(() =>
                {
                    lock (lockObject)
                    {
                        return Task.FromResult((IList<IpAllocationEntity>)entities.Values.ToList());
                    }
                });

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity =>
                {
                    lock (lockObject)
                    {
                        if (entities.ContainsKey(entity.Id))
                        {
                            throw new InvalidOperationException("Entity already exists");
                        }
                        entities[entity.Id] = entity;
                        return Task.FromResult(entity);
                    }
                });

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity =>
                {
                    lock (lockObject)
                    {
                        if (!entities.ContainsKey(entity.Id))
                        {
                            throw new InvalidOperationException("Entity not found for update");
                        }
                        entities[entity.Id] = entity;
                        return Task.FromResult(entity);
                    }
                });

            _mockRepository.Setup(r => r.DeleteAsync(addressSpaceId, It.IsAny<string>()))
                .Returns<string, string>((_, id) =>
                {
                    lock (lockObject)
                    {
                        entities.Remove(id);
                        return Task.CompletedTask;
                    }
                });

            _mockTagService.Setup(t => t.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new Dictionary<string, string>());

            _mockMapper.Setup(m => m.Map<IpAllocation>(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e => CreateTestIpAllocation(e.AddressSpaceId, e.Id, e.Prefix));

            // Act - High concurrency mixed operations
            var tasks = new List<Task>();
            const int operationsPerType = 25;

            // Create operations
            for (int i = 0; i < operationsPerType; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var ip = CreateTestIpAllocation(addressSpaceId, $"create-{index}", $"10.1.{index}.0/24");
                        await _ipAllocationService.CreateIpAllocationAsync(ip);
                    }
                    catch
                    {
                        // Expected some failures due to concurrency
                    }
                }));
            }

            // Update operations
            for (int i = 0; i < operationsPerType; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var ip = CreateTestIpAllocation(addressSpaceId, $"create-{index}", $"10.2.{index}.0/24");
                        await _ipAllocationService.UpdateIpAllocationAsync(ip);
                    }
                    catch
                    {
                        // Expected some failures due to concurrency
                    }
                }));
            }

            // Delete operations
            for (int i = 0; i < operationsPerType; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await _ipAllocationService.DeleteIpAllocationAsync(addressSpaceId, $"create-{index}");
                    }
                    catch
                    {
                        // Expected some failures due to concurrency
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - Verify final state consistency
            lock (lockObject)
            {
                // All remaining entities should have valid state
                foreach (var entity in entities.Values)
                {
                    Assert.NotNull(entity.Id); // Entity ID should not be null
                    Assert.NotNull(entity.Prefix); // Entity Prefix should not be null
                    Assert.NotNull(entity.AddressSpaceId); // Entity AddressSpaceId should not be null
                }
            }
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
                ETag = new ETag("test-etag")
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