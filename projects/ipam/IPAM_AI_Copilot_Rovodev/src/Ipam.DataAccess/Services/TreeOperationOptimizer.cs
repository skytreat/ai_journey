using Ipam.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Service for optimizing tree operations using ChildrenIds efficiently
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TreeOperationOptimizer
    {
        /// <summary>
        /// Validates ChildrenIds usage patterns and suggests optimizations
        /// </summary>
        public static class ChildrenIdsOptimizations
        {
            /// <summary>
            /// Efficiently updates ChildrenIds array without full serialization on every access
            /// </summary>
            public static void OptimizeChildrenIdsUpdate(IpAllocationEntity entity, string[] newChildrenIds)
            {
                // Only update if there's an actual change
                if (!AreArraysEqual(entity.ChildrenIds?.ToArray(), newChildrenIds))
                {
                    entity.ChildrenIds = newChildrenIds.ToList();
                    entity.ModifiedOn = DateTime.UtcNow;
                }
            }

            /// <summary>
            /// Adds a child ID efficiently without triggering unnecessary serialization
            /// </summary>
            public static bool AddChildIdOptimized(IpAllocationEntity entity, string childId)
            {
                var currentChildren = entity.ChildrenIds ?? new List<string>();
                
                // Check if child already exists
                if (currentChildren.Contains(childId))
                    return false;

                // Add child to list
                var newChildren = new List<string>(currentChildren) { childId };

                entity.ChildrenIds = newChildren;
                entity.ModifiedOn = DateTime.UtcNow;
                return true;
            }

            /// <summary>
            /// Removes a child ID efficiently
            /// </summary>
            public static bool RemoveChildIdOptimized(IpAllocationEntity entity, string childId)
            {
                var currentChildren = entity.ChildrenIds ?? new List<string>();
                
                if (!currentChildren.Contains(childId))
                    return false;

                // Create new list without the removed child
                var newChildren = new List<string>(currentChildren);
                newChildren.Remove(childId);

                entity.ChildrenIds = newChildren;
                entity.ModifiedOn = DateTime.UtcNow;
                return true;
            }

            /// <summary>
            /// Efficiently compares two string arrays for equality
            /// </summary>
            private static bool AreArraysEqual(string[] array1, string[] array2)
            {
                if (array1 == null && array2 == null) return true;
                if (array1 == null || array2 == null) return false;
                if (array1.Length != array2.Length) return false;

                for (int i = 0; i < array1.Length; i++)
                {
                    if (array1[i] != array2[i]) return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Performance analysis for ChildrenIds operations
        /// </summary>
        public static class PerformanceAnalysis
        {
            /// <summary>
            /// Analyzes the performance impact of ChildrenIds JSON serialization
            /// </summary>
            public static TreePerformanceMetrics AnalyzeChildrenIdsPerformance(IpAllocationEntity entity)
            {
                var childrenCount = entity.ChildrenIds?.Count ?? 0;
                
                return new TreePerformanceMetrics
                {
                    ChildrenCount = childrenCount,
                    EstimatedSerializationCost = EstimateSerializationCost(childrenCount),
                    RecommendedOptimization = GetOptimizationRecommendation(childrenCount)
                };
            }

            private static TimeSpan EstimateSerializationCost(int childrenCount)
            {
                // Rough estimation: 1ms per 100 children for JSON serialization
                var milliseconds = Math.Max(1, childrenCount / 100.0);
                return TimeSpan.FromMilliseconds(milliseconds);
            }

            private static string GetOptimizationRecommendation(int childrenCount)
            {
                return childrenCount switch
                {
                    <= 10 => "Optimal - No optimization needed",
                    <= 100 => "Good - Consider caching if accessed frequently",
                    <= 1000 => "Moderate - Implement lazy loading for ChildrenIds",
                    _ => "High - Consider separate children table or pagination"
                };
            }
        }

        /// <summary>
        /// Tree traversal optimizations using ChildrenIds
        /// </summary>
        public static class TreeTraversalOptimizations
        {
            /// <summary>
            /// Efficiently traverses tree using ChildrenIds without recursive database calls
            /// </summary>
            public static async Task<List<IpAllocationEntity>> GetSubtreeOptimized(
                IpAllocationEntity root,
                Func<string, string, Task<IpAllocationEntity>> getNodeFunc,
                int maxDepth = 10)
            {
                var result = new List<IpAllocationEntity> { root };
                var queue = new Queue<(IpAllocationEntity node, int depth)>();
                queue.Enqueue((root, 0));

                while (queue.Count > 0 && queue.Peek().depth < maxDepth)
                {
                    var (currentNode, depth) = queue.Dequeue();
                    
                    if (currentNode.ChildrenIds != null)
                    {
                        foreach (var childId in currentNode.ChildrenIds)
                        {
                            var childNode = await getNodeFunc(currentNode.AddressSpaceId, childId);
                            if (childNode != null)
                            {
                                result.Add(childNode);
                                queue.Enqueue((childNode, depth + 1));
                            }
                        }
                    }
                }

                return result;
            }

            /// <summary>
            /// Validates tree integrity using ChildrenIds
            /// </summary>
            public static TreeIntegrityReport ValidateTreeIntegrity(
                IEnumerable<IpAllocationEntity> allNodes)
            {
                var report = new TreeIntegrityReport();
                var nodeDict = allNodes.ToDictionary(n => n.Id, n => n);

                foreach (var node in allNodes)
                {
                    // Validate parent-child relationships
                    if (node.ChildrenIds != null)
                    {
                        foreach (var childId in node.ChildrenIds)
                        {
                            if (nodeDict.TryGetValue(childId, out var child))
                            {
                                if (child.ParentId != node.Id)
                                {
                                    report.Inconsistencies.Add(
                                        $"Node {node.Id} lists {childId} as child, but child's ParentId is {child.ParentId}");
                                }
                            }
                            else
                            {
                                report.OrphanedReferences.Add(
                                    $"Node {node.Id} references non-existent child {childId}");
                            }
                        }
                    }

                    // Validate parent references
                    if (!string.IsNullOrEmpty(node.ParentId))
                    {
                        if (nodeDict.TryGetValue(node.ParentId, out var parent))
                        {
                            if (parent.ChildrenIds == null || !parent.ChildrenIds.Contains(node.Id))
                            {
                                report.Inconsistencies.Add(
                                    $"Node {node.Id} has ParentId {node.ParentId}, but parent doesn't list it as child");
                            }
                        }
                        else
                        {
                            report.OrphanedReferences.Add(
                                $"Node {node.Id} references non-existent parent {node.ParentId}");
                        }
                    }
                }

                return report;
            }
        }
    }

    /// <summary>
    /// Performance metrics for tree operations
    /// </summary>
    public class TreePerformanceMetrics
    {
        public int ChildrenCount { get; set; }
        public TimeSpan EstimatedSerializationCost { get; set; }
        public string RecommendedOptimization { get; set; }
    }

    /// <summary>
    /// Tree integrity validation report
    /// </summary>
    public class TreeIntegrityReport
    {
        public List<string> Inconsistencies { get; set; } = new List<string>();
        public List<string> OrphanedReferences { get; set; } = new List<string>();
        public bool IsValid => Inconsistencies.Count == 0 && OrphanedReferences.Count == 0;
    }
}