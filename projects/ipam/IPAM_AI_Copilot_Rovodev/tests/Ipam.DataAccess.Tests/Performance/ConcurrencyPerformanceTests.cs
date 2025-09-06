using Xunit;
using Moq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.Performance
{
    /// <summary>
    /// Performance tests for concurrent IP tree operations
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class ConcurrencyPerformanceTests
    {
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
                
                tasks.Add(MeasureOperationAsync(() => 
                    service.CreateIpNodeAsync(addressSpaceId, cidr, tags)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - Analyze performance characteristics
            var successfulOperations = results.Where(r => r.success).ToList();
            var averageLatency = successfulOperations.Average(r => r.duration.TotalMilliseconds);
            var maxLatency = successfulOperations.Max(r => r.duration.TotalMilliseconds);
            var minLatency = successfulOperations.Min(r => r.duration.TotalMilliseconds);

            // Performance assertions
            Assert.True(successfulOperations.Count >= 8, "At least 80% operations should succeed");
            Assert.True(averageLatency < 100, $"Average latency should be < 100ms, was {averageLatency:F2}ms");
            Assert.True(maxLatency < 500, $"Max latency should be < 500ms, was {maxLatency:F2}ms");
            
            // Lock contention analysis
            var latencyVariance = maxLatency - minLatency;
            Assert.True(latencyVariance < 400, $"Latency variance should be < 400ms, was {latencyVariance:F2}ms");
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
                
                tasks.Add(MeasureOperationAsync(() => 
                    service.CreateIpNodeAsync(addressSpaceId, cidr, tags)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - Should have minimal latency variance (no contention)
            var successfulOperations = results.Where(r => r.success).ToList();
            var averageLatency = successfulOperations.Average(r => r.duration.TotalMilliseconds);
            var maxLatency = successfulOperations.Max(r => r.duration.TotalMilliseconds);
            var minLatency = successfulOperations.Min(r => r.duration.TotalMilliseconds);

            Assert.Equal(concurrentOperations, successfulOperations.Count);
            Assert.True(averageLatency < 50, $"Average latency should be < 50ms with no contention, was {averageLatency:F2}ms");
            
            var latencyVariance = maxLatency - minLatency;
            Assert.True(latencyVariance < 100, $"Latency variance should be minimal with no contention, was {latencyVariance:F2}ms");
        }

        [Fact]
        public async Task MemoryUsage_MultipleAddressSpaces_BenchmarkMemoryOverhead()
        {
            // Arrange
            var service = CreateMockedService();
            var addressSpaceCount = 100;
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Create locks for many address spaces
            var tasks = new List<Task>();
            for (int i = 0; i < addressSpaceCount; i++)
            {
                var addressSpaceId = $"space{i}";
                var cidr = "10.0.1.0/24";
                var tags = new Dictionary<string, string> { { "Test", "Value" } };
                
                tasks.Add(service.CreateIpNodeAsync(addressSpaceId, cidr, tags));
            }

            await Task.WhenAll(tasks);
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert - Memory overhead should be reasonable
            var memoryPerAddressSpace = memoryIncrease / addressSpaceCount;
            Assert.True(memoryPerAddressSpace < 1024, // Less than 1KB per address space
                $"Memory overhead per address space should be < 1KB, was {memoryPerAddressSpace} bytes");
        }

        [Theory]
        [InlineData(1, 50)]   // Single operation baseline
        [InlineData(5, 100)]  // Light contention
        [InlineData(10, 200)] // Moderate contention
        [InlineData(20, 400)] // Heavy contention
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
                
                tasks.Add(MeasureOperationAsync(() => 
                    service.CreateIpNodeAsync(addressSpaceId, cidr, tags)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - Performance should degrade gracefully
            var successfulOperations = results.Where(r => r.success).ToList();
            var averageLatency = successfulOperations.Average(r => r.duration.TotalMilliseconds);

            Assert.True(successfulOperations.Count >= concurrentOperations * 0.8, 
                "At least 80% operations should succeed");
            Assert.True(averageLatency < maxExpectedLatencyMs, 
                $"Average latency should be < {maxExpectedLatencyMs}ms for {concurrentOperations} concurrent operations, was {averageLatency:F2}ms");
        }

        [Fact]
        public async Task LockTimeout_LongRunningOperation_DoesNotBlockIndefinitely()
        {
            // Arrange
            var service = CreateMockedServiceWithDelay(TimeSpan.FromSeconds(2));
            var addressSpaceId = "space1";

            // Act - Start long-running operation and quick operation
            var longTask = service.CreateIpNodeAsync(addressSpaceId, "10.0.1.0/24", 
                new Dictionary<string, string> { { "Test", "Long" } });
            
            await Task.Delay(100); // Ensure long task starts first
            
            var quickTaskTimer = Stopwatch.StartNew();
            var quickTask = service.CreateIpNodeAsync(addressSpaceId, "10.0.2.0/24", 
                new Dictionary<string, string> { { "Test", "Quick" } });

            await Task.WhenAll(longTask, quickTask);
            quickTaskTimer.Stop();

            // Assert - Quick task should complete reasonably fast after long task
            Assert.True(quickTaskTimer.ElapsedMilliseconds < 3000, 
                $"Quick task should complete within 3 seconds, took {quickTaskTimer.ElapsedMilliseconds}ms");
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
            var ipNodeRepositoryMock = new Mock<IIpNodeRepository>();
            var tagInheritanceServiceMock = new Mock<TagInheritanceService>();

            // Setup fast, successful operations
            ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<IpNode>());

            ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), null))
                .ReturnsAsync(new List<IpNode>());

            tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((string _, Dictionary<string, string> tags) => tags);

            ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpNode>()))
                .ReturnsAsync((IpNode node) => node);

            return new ConcurrentIpTreeService(
                ipNodeRepositoryMock.Object,
                tagInheritanceServiceMock.Object);
        }

        private static ConcurrentIpTreeService CreateMockedServiceWithDelay(TimeSpan delay)
        {
            var ipNodeRepositoryMock = new Mock<IIpNodeRepository>();
            var tagInheritanceServiceMock = new Mock<TagInheritanceService>();

            ipNodeRepositoryMock.Setup(x => x.GetByPrefixAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<IpNode>());

            ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), null))
                .ReturnsAsync(new List<IpNode>());

            tagInheritanceServiceMock.Setup(x => x.ApplyTagImplications(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((string _, Dictionary<string, string> tags) => tags);

            // Add delay to simulate long-running operation
            ipNodeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<IpNode>()))
                .Returns(async (IpNode node) =>
                {
                    await Task.Delay(delay);
                    return node;
                });

            return new ConcurrentIpTreeService(
                ipNodeRepositoryMock.Object,
                tagInheritanceServiceMock.Object);
        }
    }
}