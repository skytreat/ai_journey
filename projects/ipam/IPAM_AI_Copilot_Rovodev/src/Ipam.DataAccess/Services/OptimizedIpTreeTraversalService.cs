using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Optimized IP tree traversal service using DFS and hierarchical indexing
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class OptimizedIpTreeTraversalService
    {
        private readonly IIpNodeRepository _repository;
        private readonly Dictionary<string, IpTreeIndex> _treeIndexCache;
        private readonly object _cacheLock = new object();

        public OptimizedIpTreeTraversalService(IIpNodeRepository repository)
        {
            _repository = repository;
            _treeIndexCache = new Dictionary<string, IpTreeIndex>();
        }

        /// <summary>
        /// Finds the closest parent node using optimized DFS traversal
        /// Time Complexity: O(log n) average case vs O(n) linear scan
        /// </summary>
        /// <param name="addressSpaceId">Address space identifier</param>
        /// <param name="targetCidr">Target CIDR to find parent for</param>
        /// <returns>Closest parent node or null</returns>
        public async Task<IpNode> FindClosestParentOptimizedAsync(string addressSpaceId, string targetCidr)
        {
            var targetPrefix = new Prefix(targetCidr);
            var treeIndex = await GetOrBuildTreeIndexAsync(addressSpaceId);
            
            return FindParentUsingDFS(treeIndex.RootNodes, targetPrefix);
        }

        /// <summary>
        /// DFS traversal to find the closest parent
        /// Traverses only relevant branches of the tree
        /// </summary>
        private IpNode FindParentUsingDFS(List<IpTreeNode> nodes, Prefix targetPrefix)
        {
            IpNode closestParent = null;
            int maxMatchingLength = -1;

            foreach (var treeNode in nodes)
            {
                try
                {
                    var nodePrefix = new Prefix(treeNode.Node.Prefix);
                    
                    // Check if this node could be a parent
                    if (nodePrefix.IsSupernetOf(targetPrefix))
                    {
                        // This node is a potential parent
                        if (nodePrefix.PrefixLength > maxMatchingLength)
                        {
                            closestParent = treeNode.Node;
                            maxMatchingLength = nodePrefix.PrefixLength;
                        }

                        // Recursively search children for a closer parent
                        // Only search children that could contain the target
                        var childResult = FindParentUsingDFS(
                            treeNode.Children.Where(child => 
                            {
                                try
                                {
                                    var childPrefix = new Prefix(child.Node.Prefix);
                                    return childPrefix.IsSupernetOf(targetPrefix);
                                }
                                catch
                                {
                                    return false;
                                }
                            }).ToList(), 
                            targetPrefix);

                        if (childResult != null)
                        {
                            var childPrefix = new Prefix(childResult.Prefix);
                            if (childPrefix.PrefixLength > maxMatchingLength)
                            {
                                closestParent = childResult;
                                maxMatchingLength = childPrefix.PrefixLength;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip invalid prefixes
                    continue;
                }
            }

            return closestParent;
        }

        /// <summary>
        /// Alternative implementation using iterative DFS with stack
        /// More memory efficient for very deep trees
        /// </summary>
        public async Task<IpNode> FindClosestParentIterativeAsync(string addressSpaceId, string targetCidr)
        {
            var targetPrefix = new Prefix(targetCidr);
            var treeIndex = await GetOrBuildTreeIndexAsync(addressSpaceId);
            
            var stack = new Stack<IpTreeNode>();
            foreach (var root in treeIndex.RootNodes)
            {
                stack.Push(root);
            }

            IpNode closestParent = null;
            int maxMatchingLength = -1;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                
                try
                {
                    var currentPrefix = new Prefix(current.Node.Prefix);
                    
                    if (currentPrefix.IsSupernetOf(targetPrefix))
                    {
                        // This is a potential parent
                        if (currentPrefix.PrefixLength > maxMatchingLength)
                        {
                            closestParent = current.Node;
                            maxMatchingLength = currentPrefix.PrefixLength;
                        }

                        // Add children to stack for further exploration
                        // Only add children that could potentially contain the target
                        foreach (var child in current.Children)
                        {
                            try
                            {
                                var childPrefix = new Prefix(child.Node.Prefix);
                                if (childPrefix.IsSupernetOf(targetPrefix))
                                {
                                    stack.Push(child);
                                }
                            }
                            catch
                            {
                                // Skip invalid prefixes
                                continue;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip invalid prefixes
                    continue;
                }
            }

            return closestParent;
        }

        /// <summary>
        /// Builds or retrieves cached tree index for an address space
        /// </summary>
        private async Task<IpTreeIndex> GetOrBuildTreeIndexAsync(string addressSpaceId)
        {
            lock (_cacheLock)
            {
                if (_treeIndexCache.TryGetValue(addressSpaceId, out var cachedIndex))
                {
                    // Check if cache is still valid (simple time-based invalidation)
                    if (DateTime.UtcNow - cachedIndex.CreatedAt < TimeSpan.FromMinutes(5))
                    {
                        return cachedIndex;
                    }
                    else
                    {
                        _treeIndexCache.Remove(addressSpaceId);
                    }
                }
            }

            // Build new index
            var allNodes = await _repository.GetChildrenAsync(addressSpaceId, null);
            var treeIndex = BuildTreeIndex(allNodes.ToList());
            
            lock (_cacheLock)
            {
                _treeIndexCache[addressSpaceId] = treeIndex;
            }

            return treeIndex;
        }

        /// <summary>
        /// Builds hierarchical tree index from flat list of nodes
        /// </summary>
        private IpTreeIndex BuildTreeIndex(List<IpNode> allNodes)
        {
            var nodeMap = new Dictionary<string, IpTreeNode>();
            var rootNodes = new List<IpTreeNode>();

            // First pass: Create tree nodes
            foreach (var node in allNodes)
            {
                nodeMap[node.Id] = new IpTreeNode
                {
                    Node = node,
                    Children = new List<IpTreeNode>()
                };
            }

            // Second pass: Build parent-child relationships
            foreach (var node in allNodes)
            {
                var treeNode = nodeMap[node.Id];
                
                if (string.IsNullOrEmpty(node.ParentId))
                {
                    // Root node
                    rootNodes.Add(treeNode);
                }
                else if (nodeMap.TryGetValue(node.ParentId, out var parentTreeNode))
                {
                    // Add to parent's children
                    parentTreeNode.Children.Add(treeNode);
                    treeNode.Parent = parentTreeNode;
                }
                else
                {
                    // Parent not found, treat as root
                    rootNodes.Add(treeNode);
                }
            }

            // Sort root nodes by prefix length (shortest first for optimal traversal)
            rootNodes.Sort((a, b) => 
            {
                try
                {
                    var prefixA = new Prefix(a.Node.Prefix);
                    var prefixB = new Prefix(b.Node.Prefix);
                    return prefixA.PrefixLength.CompareTo(prefixB.PrefixLength);
                }
                catch
                {
                    return 0;
                }
            });

            return new IpTreeIndex
            {
                RootNodes = rootNodes,
                NodeMap = nodeMap,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Optimized parent finding using prefix-based binary search approach
        /// For very large trees with well-structured CIDR allocations
        /// </summary>
        public async Task<IpNode> FindClosestParentBinarySearchAsync(string addressSpaceId, string targetCidr)
        {
            var targetPrefix = new Prefix(targetCidr);
            var allNodes = await _repository.GetChildrenAsync(addressSpaceId, null);
            
            // Sort nodes by prefix for binary search approach
            var sortedNodes = allNodes
                .Where(n => 
                {
                    try
                    {
                        var prefix = new Prefix(n.Prefix);
                        return prefix.IsSupernetOf(targetPrefix);
                    }
                    catch
                    {
                        return false;
                    }
                })
                .OrderByDescending(n => new Prefix(n.Prefix).PrefixLength)
                .ToList();

            // Return the first (longest prefix) that contains the target
            return sortedNodes.FirstOrDefault();
        }

        /// <summary>
        /// Invalidates the tree index cache for an address space
        /// Call this when nodes are added/removed/modified
        /// </summary>
        public void InvalidateCache(string addressSpaceId)
        {
            lock (_cacheLock)
            {
                _treeIndexCache.Remove(addressSpaceId);
            }
        }

        /// <summary>
        /// Invalidates all cached tree indexes
        /// </summary>
        public void InvalidateAllCaches()
        {
            lock (_cacheLock)
            {
                _treeIndexCache.Clear();
            }
        }

        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            lock (_cacheLock)
            {
                return new CacheStatistics
                {
                    CachedAddressSpaces = _treeIndexCache.Count,
                    TotalCachedNodes = _treeIndexCache.Values.Sum(index => index.NodeMap.Count),
                    OldestCacheEntry = _treeIndexCache.Values.Any() 
                        ? _treeIndexCache.Values.Min(index => index.CreatedAt)
                        : (DateTime?)null,
                    NewestCacheEntry = _treeIndexCache.Values.Any()
                        ? _treeIndexCache.Values.Max(index => index.CreatedAt)
                        : (DateTime?)null
                };
            }
        }
    }

    /// <summary>
    /// Represents a node in the IP tree hierarchy
    /// </summary>
    public class IpTreeNode
    {
        public IpNode Node { get; set; }
        public List<IpTreeNode> Children { get; set; } = new List<IpTreeNode>();
        public IpTreeNode Parent { get; set; }
    }

    /// <summary>
    /// Cached tree index for an address space
    /// </summary>
    public class IpTreeIndex
    {
        public List<IpTreeNode> RootNodes { get; set; } = new List<IpTreeNode>();
        public Dictionary<string, IpTreeNode> NodeMap { get; set; } = new Dictionary<string, IpTreeNode>();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Cache performance statistics
    /// </summary>
    public class CacheStatistics
    {
        public int CachedAddressSpaces { get; set; }
        public int TotalCachedNodes { get; set; }
        public DateTime? OldestCacheEntry { get; set; }
        public DateTime? NewestCacheEntry { get; set; }
    }
}