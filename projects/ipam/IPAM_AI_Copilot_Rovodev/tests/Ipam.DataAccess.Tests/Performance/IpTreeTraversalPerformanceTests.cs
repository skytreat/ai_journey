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
    /// Performance comparison tests for IP tree traversal algorithms
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpTreeTraversalPerformanceTests
    {
        [Theory]
        [InlineData(100)]    // Small tree
        [InlineData(1000)]   // Medium tree
        [InlineData(10000)]  // Large tree
        public async Task FindParent_LinearVsDFS_PerformanceComparison(int nodeCount)
        {
            // Arrange
            var nodes = GenerateHierarchicalNodes(nodeCount);
            var targetCidr = "10.1.50.0/24"; // Deep in the tree
            
            var repositoryMock = CreateMockRepository(nodes);
            var optimizedService = new OptimizedIpTreeTraversalService(repositoryMock.Object);

            // Act & Measure - Linear Search (Current Implementation)
            var linearStopwatch = Stopwatch.StartNew();
            var linearResult = await FindParentLinear(nodes, targetCidr);
            linearStopwatch.Stop();

            // Act & Measure - DFS Optimized
            var dfsStopwatch = Stopwatch.StartNew();
            var dfsResult = await optimizedService.FindClosestParentOptimizedAsync("space1", targetCidr);
            dfsStopwatch.Stop();

            // Act & Measure - Iterative DFS
            var iterativeStopwatch = Stopwatch.StartNew();
            var iterativeResult = await optimizedService.FindClosestParentIterativeAsync("space1", targetCidr);
            iterativeStopwatch.Stop();

            // Act & Measure - Binary Search
            var binaryStopwatch = Stopwatch.StartNew();
            var binaryResult = await optimizedService.FindClosestParentBinarySearchAsync("space1", targetCidr);
            binaryStopwatch.Stop();

            // Assert - Results should be the same
            Assert.Equal(linearResult?.Id, dfsResult?.Id);
            Assert.Equal(linearResult?.Id, iterativeResult?.Id);
            Assert.Equal(linearResult?.Id, binaryResult?.Id);

            // Performance Analysis
            var linearTime = linearStopwatch.ElapsedMilliseconds;
            var dfsTime = dfsStopwatch.ElapsedMilliseconds;
            var iterativeTime = iterativeStopwatch.ElapsedMilliseconds;
            var binaryTime = binaryStopwatch.ElapsedMilliseconds;

            // Log performance results
            Console.WriteLine($"Node Count: {nodeCount}");
            Console.WriteLine($"Linear Search: {linearTime}ms");
            Console.WriteLine($"DFS Recursive: {dfsTime}ms");
            Console.WriteLine($"DFS Iterative: {iterativeTime}ms");
            Console.WriteLine($"Binary Search: {binaryTime}ms");
            Console.WriteLine($"DFS Improvement: {(double)linearTime / Math.Max(dfsTime, 1):F2}x faster");
            Console.WriteLine($"Binary Improvement: {(double)linearTime / Math.Max(binaryTime, 1):F2}x faster");

            // Performance assertions based on expected complexity
            if (nodeCount >= 1000)
            {
                // For large trees, DFS should be significantly faster
                Assert.True(dfsTime <= linearTime, $"DFS should be faster than linear for {nodeCount} nodes");
                Assert.True(binaryTime <= linearTime, $"Binary search should be faster than linear for {nodeCount} nodes");
            }
        }

        [Fact]
        public async Task CacheEffectiveness_RepeatedQueries_ShowsPerformanceGain()
        {
            // Arrange
            var nodes = GenerateHierarchicalNodes(5000);
            var repositoryMock = CreateMockRepository(nodes);
            var optimizedService = new OptimizedIpTreeTraversalService(repositoryMock.Object);

            var targetCidrs = new[]
            {
                "10.1.1.0/24", "10.1.2.0/24", "10.1.3.0/24",
                "10.2.1.0/24", "10.2.2.0/24", "10.3.1.0/24"
            };

            // Act - First round (cache miss)
            var firstRoundStopwatch = Stopwatch.StartNew();
            foreach (var cidr in targetCidrs)
            {
                await optimizedService.FindClosestParentOptimizedAsync("space1", cidr);
            }
            firstRoundStopwatch.Stop();

            // Act - Second round (cache hit)
            var secondRoundStopwatch = Stopwatch.StartNew();
            foreach (var cidr in targetCidrs)
            {
                await optimizedService.FindClosestParentOptimizedAsync("space1", cidr);
            }
            secondRoundStopwatch.Stop();

            // Assert - Second round should be significantly faster
            var firstRoundTime = firstRoundStopwatch.ElapsedMilliseconds;
            var secondRoundTime = secondRoundStopwatch.ElapsedMilliseconds;
            var improvement = (double)firstRoundTime / Math.Max(secondRoundTime, 1);

            Console.WriteLine($"First round (cache miss): {firstRoundTime}ms");
            Console.WriteLine($"Second round (cache hit): {secondRoundTime}ms");
            Console.WriteLine($"Cache improvement: {improvement:F2}x faster");

            Assert.True(improvement >= 2.0, $"Cache should provide at least 2x improvement, got {improvement:F2}x");
        }

        [Theory]
        [InlineData(3)]   // Shallow tree
        [InlineData(6)]   // Medium depth
        [InlineData(10)]  // Deep tree
        public async Task TreeDepth_Impact_OnTraversalPerformance(int maxDepth)
        {
            // Arrange
            var nodes = GenerateDeepTree(maxDepth);
            var targetCidr = GenerateDeepestNodeCidr(maxDepth);
            
            var repositoryMock = CreateMockRepository(nodes);
            var optimizedService = new OptimizedIpTreeTraversalService(repositoryMock.Object);

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            var result = await optimizedService.FindClosestParentOptimizedAsync("space1", targetCidr);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Tree depth {maxDepth}: {elapsedTime}ms");

            // Performance should scale logarithmically with depth
            Assert.True(elapsedTime < maxDepth * 10, 
                $"Performance should scale well with depth. Depth {maxDepth} took {elapsedTime}ms");
        }

        [Fact]
        public async Task MemoryUsage_TreeIndexCache_RemainsReasonable()
        {
            // Arrange
            var nodes = GenerateHierarchicalNodes(10000);
            var repositoryMock = CreateMockRepository(nodes);
            var optimizedService = new OptimizedIpTreeTraversalService(repositoryMock.Object);

            var initialMemory = GC.GetTotalMemory(true);

            // Act - Build cache for multiple address spaces
            for (int i = 0; i < 10; i++)
            {
                await optimizedService.FindClosestParentOptimizedAsync($"space{i}", "10.1.1.0/24");
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            var stats = optimizedService.GetCacheStatistics();
            Assert.Equal(10, stats.CachedAddressSpaces);

            var memoryPerSpace = memoryIncrease / 10;
            Console.WriteLine($"Memory per address space: {memoryPerSpace / 1024:F2} KB");
            Console.WriteLine($"Total cached nodes: {stats.TotalCachedNodes}");

            // Memory usage should be reasonable (< 1MB per address space for 10k nodes)
            Assert.True(memoryPerSpace < 1024 * 1024, 
                $"Memory per address space should be < 1MB, was {memoryPerSpace / 1024:F2} KB");
        }

        [Fact]
        public async Task ConcurrentAccess_CacheConsistency_ThreadSafe()
        {
            // Arrange
            var nodes = GenerateHierarchicalNodes(1000);
            var repositoryMock = CreateMockRepository(nodes);
            var optimizedService = new OptimizedIpTreeTraversalService(repositoryMock.Object);

            var tasks = new List<Task<IpNode>>();
            var targetCidrs = new[]
            {
                "10.1.1.0/24", "10.1.2.0/24", "10.1.3.0/24",
                "10.2.1.0/24", "10.2.2.0/24", "10.3.1.0/24"
            };

            // Act - Concurrent access to same address space
            for (int i = 0; i < 20; i++)
            {
                var cidr = targetCidrs[i % targetCidrs.Length];
                tasks.Add(optimizedService.FindClosestParentOptimizedAsync("space1", cidr));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All results should be consistent
            Assert.All(results, result => Assert.NotNull(result));
            
            // Group by target CIDR and verify consistency
            for (int i = 0; i < targetCidrs.Length; i++)
            {
                var expectedResult = results[i];
                for (int j = i + targetCidrs.Length; j < results.Length; j += targetCidrs.Length)
                {
                    Assert.Equal(expectedResult.Id, results[j].Id);
                }
            }
        }

        private static List<IpNode> GenerateHierarchicalNodes(int count)
        {
            var nodes = new List<IpNode>();
            var random = new Random(42); // Fixed seed for reproducible tests

            // Create root nodes
            nodes.Add(new IpNode
            {
                Id = "root-ipv4",
                PartitionKey = "space1",
                RowKey = "root-ipv4",
                Prefix = "0.0.0.0/0",
                ParentId = null
            });

            // Create hierarchical structure
            for (int i = 1; i < count; i++)
            {
                var depth = random.Next(1, 6); // 1-5 levels deep
                var prefix = GenerateRandomPrefix(depth, random);
                var parentId = FindSuitableParent(nodes, prefix);

                nodes.Add(new IpNode
                {
                    Id = $"node-{i}",
                    PartitionKey = "space1",
                    RowKey = $"node-{i}",
                    Prefix = prefix,
                    ParentId = parentId
                });
            }

            return nodes;
        }

        private static List<IpNode> GenerateDeepTree(int maxDepth)
        {
            var nodes = new List<IpNode>();

            // Create a single deep branch
            for (int depth = 0; depth < maxDepth; depth++)
            {
                var prefixLength = depth * 4; // /0, /4, /8, /12, etc.
                if (prefixLength > 24) prefixLength = 24;

                var prefix = $"10.0.0.0/{prefixLength}";
                var parentId = depth > 0 ? $"node-{depth - 1}" : null;

                nodes.Add(new IpNode
                {
                    Id = $"node-{depth}",
                    PartitionKey = "space1",
                    RowKey = $"node-{depth}",
                    Prefix = prefix,
                    ParentId = parentId
                });
            }

            return nodes;
        }

        private static string GenerateDeepestNodeCidr(int maxDepth)
        {
            return $"10.0.0.1/{Math.Min(maxDepth * 4 + 4, 32)}";
        }

        private static string GenerateRandomPrefix(int depth, Random random)
        {
            var prefixLength = Math.Min(8 + depth * 4, 24);
            var octet1 = 10;
            var octet2 = random.Next(0, 256);
            var octet3 = random.Next(0, 256);
            var octet4 = 0;

            return $"{octet1}.{octet2}.{octet3}.{octet4}/{prefixLength}";
        }

        private static string FindSuitableParent(List<IpNode> existingNodes, string targetPrefix)
        {
            try
            {
                var target = new Prefix(targetPrefix);
                var suitableParents = existingNodes
                    .Where(n =>
                    {
                        try
                        {
                            var nodePrefix = new Prefix(n.Prefix);
                            return nodePrefix.IsSupernetOf(target);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .OrderByDescending(n => new Prefix(n.Prefix).PrefixLength)
                    .ToList();

                return suitableParents.FirstOrDefault()?.Id;
            }
            catch
            {
                return null;
            }
        }

        private static Mock<IIpNodeRepository> CreateMockRepository(List<IpNode> nodes)
        {
            var mock = new Mock<IIpNodeRepository>();
            
            mock.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), null))
                .ReturnsAsync(nodes);

            return mock;
        }

        private static async Task<IpNode> FindParentLinear(List<IpNode> nodes, string targetCidr)
        {
            // Simulate the current linear implementation
            var targetPrefix = new Prefix(targetCidr);
            var closestParent = default(IpNode);
            var maxMatchingLength = -1;

            foreach (var node in nodes)
            {
                try
                {
                    var nodePrefix = new Prefix(node.Prefix);
                    if (nodePrefix.IsSupernetOf(targetPrefix) &&
                        nodePrefix.PrefixLength > maxMatchingLength)
                    {
                        closestParent = node;
                        maxMatchingLength = nodePrefix.PrefixLength;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return await Task.FromResult(closestParent);
        }
    }
}