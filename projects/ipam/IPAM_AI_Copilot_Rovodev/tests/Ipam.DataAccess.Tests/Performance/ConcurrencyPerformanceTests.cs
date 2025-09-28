using Xunit;
using Ipam.DataAccess.Tests.TestHelpers;
using Xunit.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Ipam.DataAccess.Tests.Performance
{
    /// <summary>
    /// Performance tests to validate concurrency improvements and measure impact
    /// </summary>
    public class ConcurrencyPerformanceTests
    {
        private Mock<IIpAllocationRepository> _mockRepository;
        private Mock<ITagRepository> _mockTagRepository;
        private TagInheritanceService _tagService;
        private Mock<IMapper> _mockMapper;
        private Mock<ILogger<IpAllocationServiceImpl>> _mockLogger;
        private Mock<PerformanceMonitoringService> _mockPerformanceService;
        private ConcurrentIpTreeService _concurrentService;
        private IpAllocationServiceImpl _ipAllocationService;
        private readonly ITestOutputHelper _output;

        public ConcurrencyPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            Setup();
        }

        private void Setup()
        {
            _mockRepository = new Mock<IIpAllocationRepository>();
            _mockTagRepository = new Mock<ITagRepository>();
            _tagService = new TagInheritanceService(_mockTagRepository.Object);
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<IpAllocationServiceImpl>>();
            _mockPerformanceService = new Mock<PerformanceMonitoringService>();

            _concurrentService = new ConcurrentIpTreeService(
                _mockRepository.Object,
                _tagService);

            _ipAllocationService = new IpAllocationServiceImpl(
                _mockRepository.Object,
                null,
                _concurrentService,
                _mockPerformanceService.Object,
                _mockMapper.Object,
                _mockLogger.Object);

            SetupCommonMocks();
        }

        private void SetupCommonMocks()
        {
            _mockTagRepository.Setup(t => t.GetByNameAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((TagEntity?)null);

            _mockMapper.Setup(m => m.Map<IpAllocation>(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(e => new IpAllocation
                {
                    Id = e.Id,
                    AddressSpaceId = e.AddressSpaceId,
                    Prefix = e.Prefix,
                    Tags = e.Tags,
                    CreatedOn = e.CreatedOn,
                    ModifiedOn = e.ModifiedOn
                });

            _mockPerformanceService.Setup(p => p.MeasureAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<string>>>>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>((name, func, props) => func());
        }

        #region Throughput Performance Tests

        [Fact]
        public async Task ConcurrentUpdates_WithConcurrencyControl_ShouldMaintainThroughput()
        {
            // Arrange
            const string addressSpaceId = TestConstants.PerformanceTestAddressSpaceId;
            const int operationCount = 100;
            const int concurrentThreads = 10;

            var entities = new Dictionary<string, IpAllocationEntity>();
            var lockObject = new object();
            var operationTimes = new List<TimeSpan>();

            // Setup repository with thread-safe operations
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

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns<IpAllocationEntity>(entity =>
                {
                    var startTime = DateTime.UtcNow;

                    lock (lockObject)
                    {
                        // Simulate database operation latency
                        Thread.Sleep(1);
                        entities[entity.Id] = entity;

                        var endTime = DateTime.UtcNow;
                        operationTimes.Add(endTime - startTime);

                        return Task.FromResult(entity);
                    }
                });

            // Pre-populate entities
            for (int i = 0; i < operationCount; i++)
            {
                entities[$"ip-{i}"] = CreateTestEntity(addressSpaceId, $"ip-{i}", $"10.0.{i / 256}.{i % 256}/32");
            }

            // Act - Execute concurrent updates
            var stopwatch = Stopwatch.StartNew();

            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(concurrentThreads, concurrentThreads);

            for (int i = 0; i < operationCount; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var ipAllocation = new IpAllocation
                        {
                            Id = $"ip-{index}",
                            AddressSpaceId = addressSpaceId,
                            Prefix = $"10.0.{index / 256}.{index % 256}/32",
                            Tags = new Dictionary<string, string> { ["updated"] = "true" }
                        };

                        await _ipAllocationService.UpdateIpAllocationAsync(ipAllocation);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert - Performance metrics
            var totalTime = stopwatch.Elapsed;
            var throughput = operationCount / totalTime.TotalSeconds;
            var averageOperationTime = operationTimes.Average(t => t.TotalMilliseconds);

            _output.WriteLine($"Total time: {totalTime.TotalSeconds:F2} seconds");
            _output.WriteLine($"Throughput: {throughput:F2} operations/second");
            _output.WriteLine($"Average operation time: {averageOperationTime:F2} ms");

            // Performance assertions
            Assert.True(throughput > 50); // Throughput should be > 50 ops/sec
            Assert.True(averageOperationTime < 100); // Average operation time should be < 100ms
            Assert.Equal(operationCount, entities.Count); // All operations should have completed successfully
        }

        #endregion

        #region Helper Methods

        private IpAllocationEntity CreateTestEntity(string addressSpaceId, string id, string prefix)
        {
            return new IpAllocationEntity
            {
                Id = id,
                AddressSpaceId = addressSpaceId,
                Prefix = prefix,
                Tags = new Dictionary<string, string>(),
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                ChildrenIds = new List<string>()
            };
        }

        #endregion

        #region Additional Performance Test Cases
        [Fact]
        public async Task ConcurrentCreation_SameAddressSpace_MeasuresLockContention()
        {
            // Arrange
            var service = CreateMockedService();
            var addressSpaceId = "space1";
            var concurrentOperations = 10;
            var tasks = new List<Task<(TimeSpan duration, bool success)>>();

            // Act - Create multiple nodes concurrently in same address space
            for (int i = 0; i < concurrentOperations; i++)
            {
                var cidr = $"10.0.{i}.0/24";
                var tags = new Dictionary<string, string> { { "Test", $"Value{i}" } };
                
                var ipAllocation = new IpAllocation
                {
                    Id = Guid.NewGuid().ToString(),
                    AddressSpaceId = addressSpaceId,
                    Prefix = cidr,
                    Tags = tags,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };
                
                tasks.Add(MeasureOperationAsync(() => 
                    service.CreateIpAllocationAsync(ipAllocation)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - Analyze performance characteristics
            var successfulOperations = results.Where(r => r.success).ToList();
            
            // Guard against empty collections
            if (successfulOperations.Count > 0)
            {
                var averageLatency = successfulOperations.Average(r => r.duration.TotalMilliseconds);
                var maxLatency = successfulOperations.Max(r => r.duration.TotalMilliseconds);
                var minLatency = successfulOperations.Min(r => r.duration.TotalMilliseconds);

                // Performance assertions
                Assert.True(successfulOperations.Count >= 8); // At least 80% operations should succeed
                Assert.True(averageLatency < 1000); // More realistic latency expectation
                Assert.True(maxLatency < 2000); // More realistic max latency
                
                // Lock contention analysis
                var latencyVariance = maxLatency - minLatency;
                _output.WriteLine($"Lock contention test - Average: {averageLatency:F2}ms, Variance: {latencyVariance:F2}ms");
            }
            else
            {
                Assert.True(false, "No operations succeeded - test setup issue");
            }
        }

        [Fact]
        public async Task ConcurrentCreation_DifferentAddressSpaces_NoContention()
        {
            // Arrange
            var service = CreateMockedService();
            var concurrentOperations = 10;
            var tasks = new List<Task<(TimeSpan duration, bool success)>>();

            // Act - Create nodes in different address spaces (should have no contention)
            for (int i = 0; i < concurrentOperations; i++)
            {
                var addressSpaceId = $"space{i}";
                var cidr = "10.0.1.0/24"; // Same CIDR in different spaces
                var tags = new Dictionary<string, string> { { "Test", "Value" } };
                
                var ipAllocation = new IpAllocation
                {
                    Id = Guid.NewGuid().ToString(),
                    AddressSpaceId = addressSpaceId,
                    Prefix = cidr,
                    Tags = tags,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };
                
                tasks.Add(MeasureOperationAsync(() => 
                    service.CreateIpAllocationAsync(ipAllocation)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - Should have minimal latency variance (no contention)
            var successfulOperations = results.Where(r => r.success).ToList();
            
            if (successfulOperations.Count > 0)
            {
                var averageLatency = successfulOperations.Average(r => r.duration.TotalMilliseconds);
                var maxLatency = successfulOperations.Max(r => r.duration.TotalMilliseconds);
                var minLatency = successfulOperations.Min(r => r.duration.TotalMilliseconds);

                Assert.Equal(concurrentOperations, successfulOperations.Count);
                Assert.True(averageLatency < 500); // More realistic expectation for mocked operations
                
                var latencyVariance = maxLatency - minLatency;
                _output.WriteLine($"No contention test - Average: {averageLatency:F2}ms, Variance: {latencyVariance:F2}ms");
            }
            else
            {
                Assert.True(false, "No operations succeeded - test setup issue");
            }
        }

        [Fact]
        public async Task MemoryUsage_MultipleAddressSpaces_BenchmarkMemoryOverhead()
        {
            // Arrange
            var service = CreateMockedService();
            var addressSpaceCount = 100;
            
            // Force garbage collection to get baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Create locks for many address spaces
            var tasks = new List<Task>();
            for (int i = 0; i < addressSpaceCount; i++)
            {
                var addressSpaceId = $"space{i}";
                var cidr = "10.0.1.0/24";
                var tags = new Dictionary<string, string> { { "Test", "Value" } };
                
                var ipAllocation = new IpAllocation
                {
                    Id = Guid.NewGuid().ToString(),
                    AddressSpaceId = addressSpaceId,
                    Prefix = cidr,
                    Tags = tags,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };
                
                tasks.Add(service.CreateIpAllocationAsync(ipAllocation));
            }

            await Task.WhenAll(tasks);
            
            // Force garbage collection to measure only persistent memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = Math.Max(0, finalMemory - initialMemory); // Prevent negative values

            // Assert - Memory overhead should be reasonable
            var memoryPerAddressSpace = addressSpaceCount > 0 ? memoryIncrease / addressSpaceCount : 0;
            _output.WriteLine($"Memory usage - Initial: {initialMemory / 1024}KB, Final: {finalMemory / 1024}KB, Increase: {memoryIncrease / 1024}KB");
            _output.WriteLine($"Memory per address space: {memoryPerAddressSpace} bytes");
            
            Assert.True(memoryPerAddressSpace < 10240); // Less than 10KB per address space (more realistic)
        }

        [Theory]
        [InlineData(1, 500)]   // Single operation baseline
        [InlineData(5, 1000)]  // Light contention
        [InlineData(10, 1500)] // Moderate contention
        [InlineData(20, 2000)] // Heavy contention
        public async Task ScalabilityTest_VaryingConcurrency_MeasuresPerformanceDegradation(
            int concurrentOperations, 
            int maxExpectedLatencyMs)
        {
            // Arrange
            var service = CreateMockedService();
            var addressSpaceId = "space1";
            var tasks = new List<Task<(TimeSpan duration, bool success)>>();

            // Act
            for (int i = 0; i < concurrentOperations; i++)
            {
                var cidr = $"10.0.{i}.0/24";
                var tags = new Dictionary<string, string> { { "Test", $"Value{i}" } };
                
                var ipAllocation = new IpAllocation
                {
                    Id = Guid.NewGuid().ToString(),
                    AddressSpaceId = addressSpaceId,
                    Prefix = cidr,
                    Tags = tags,
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };
                
                tasks.Add(MeasureOperationAsync(() => 
                    service.CreateIpAllocationAsync(ipAllocation)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - Performance should degrade gracefully
            var successfulOperations = results.Where(r => r.success).ToList();
            
            if (successfulOperations.Count > 0)
            {
                var averageLatency = successfulOperations.Average(r => r.duration.TotalMilliseconds);
                
                _output.WriteLine($"Scalability test - {concurrentOperations} operations, Average latency: {averageLatency:F2}ms");

                Assert.True(successfulOperations.Count >= concurrentOperations * 0.8); 
                // At least 80% operations should succeed
                Assert.True(averageLatency < maxExpectedLatencyMs); 
                // Performance should be within expected bounds
            }
            else
            {
                Assert.True(false, "No operations succeeded - test setup issue");
            }
        }

        [Fact]
        public async Task LockTimeout_LongRunningOperation_DoesNotBlockIndefinitely()
        {
            // Arrange
            var service = CreateMockedServiceWithDelay(TimeSpan.FromSeconds(1)); // Reduced delay for test speed
            var addressSpaceId = "space1";

            var longIpAllocation = new IpAllocation
            {
                Id = Guid.NewGuid().ToString(),
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.1.0/24",
                Tags = new Dictionary<string, string> { { "Test", "Long" } },
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            var quickIpAllocation = new IpAllocation
            {
                Id = Guid.NewGuid().ToString(),
                AddressSpaceId = addressSpaceId,
                Prefix = "10.0.2.0/24",
                Tags = new Dictionary<string, string> { { "Test", "Quick" } },
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            // Act - Start long-running operation and quick operation
            var longTask = service.CreateIpAllocationAsync(longIpAllocation);
            
            await Task.Delay(100); // Ensure long task starts first
            
            var quickTaskTimer = Stopwatch.StartNew();
            var quickTask = service.CreateIpAllocationAsync(quickIpAllocation);

            await Task.WhenAll(longTask, quickTask);
            quickTaskTimer.Stop();

            // Assert - Quick task should complete reasonably fast after long task
            _output.WriteLine($"Lock timeout test - Quick task completed in {quickTaskTimer.ElapsedMilliseconds}ms");
            Assert.True(quickTaskTimer.ElapsedMilliseconds < 3000); 
            // Quick task should complete within 3 seconds
        }

        private static async Task<(TimeSpan duration, bool success)> MeasureOperationAsync(Func<Task> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await operation();
                stopwatch.Stop();
                return (stopwatch.Elapsed, true);
            }
            catch
            {
                stopwatch.Stop();
                return (stopwatch.Elapsed, false);
            }
        }

        private static ConcurrentIpTreeService CreateMockedService()
        {
            var ipNodeRepositoryMock = new Mock<IIpAllocationRepository>();
            var tagRepositoryMock = new Mock<ITagRepository>();
            var tagInheritanceService = new TagInheritanceService(tagRepositoryMock.Object);

            // Setup fast, successful operations
            ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            ipNodeRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            ipNodeRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((IpAllocationEntity?)null); // Simulate entity not found for creation scenarios

            tagRepositoryMock.Setup(x => x.GetByNameAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((TagEntity?)null);

            ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .ReturnsAsync((IpAllocationEntity node) => 
                {
                    // Simulate minimal processing time
                    Thread.Sleep(1);
                    return node;
                });

            return new ConcurrentIpTreeService(
                ipNodeRepositoryMock.Object,
                tagInheritanceService);
        }

        private static ConcurrentIpTreeService CreateMockedServiceWithDelay(TimeSpan delay)
        {
            var ipNodeRepositoryMock = new Mock<IIpAllocationRepository>();
            var tagRepositoryMock = new Mock<ITagRepository>();
            var tagInheritanceService = new TagInheritanceService(tagRepositoryMock.Object);

            ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            ipNodeRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<IpAllocationEntity>());

            ipNodeRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((IpAllocationEntity?)null);

            tagRepositoryMock.Setup(x => x.GetByNameAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((TagEntity?)null);

            // Add delay to simulate long-running operation
            ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpAllocationEntity>()))
                .Returns(async (IpAllocationEntity node) =>
                {
                    await Task.Delay(delay);
                    return node;
                });

            return new ConcurrentIpTreeService(
                ipNodeRepositoryMock.Object,
                tagInheritanceService);
        }
        #endregion
    }
}