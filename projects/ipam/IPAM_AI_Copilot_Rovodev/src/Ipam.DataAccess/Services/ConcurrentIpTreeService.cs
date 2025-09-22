using Ipam.DataAccess.Interfaces;
using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Validation;
using Ipam.DataAccess.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Ipam.ServiceContract.Models;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Concurrent-safe service for managing IP tree structure and relationships
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class ConcurrentIpTreeService
    {
        private readonly IIpAllocationRepository _ipAllocationRepository;
        private readonly TagInheritanceService _tagInheritanceService;
        private readonly SemaphoreSlim _creationSemaphore;
        private readonly Dictionary<string, SemaphoreSlim> _addressSpaceLocks;
        private readonly object _lockDictionary = new object();

        public ConcurrentIpTreeService(
            IIpAllocationRepository ipAllocationRepository,
            TagInheritanceService tagInheritanceService)
        {
            _ipAllocationRepository = ipAllocationRepository;
            _tagInheritanceService = tagInheritanceService;
            _creationSemaphore = new SemaphoreSlim(10, 10); // Allow up to 10 concurrent creations
            _addressSpaceLocks = new Dictionary<string, SemaphoreSlim>();
        }

        /// <summary>
        /// Creates an IP node with concurrent-safe parent detection and tag validation
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="cidr">The CIDR notation</param>
        /// <param name="tags">The tags for the IP node</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created IP node</returns>
        public async Task<IpAllocationEntity> CreateIpAllocationAsync(
            IpAllocation ipAllocation,
            CancellationToken cancellationToken = default)
        {
            // Get address space specific lock
            var addressSpaceLock = GetAddressSpaceLock(ipAllocation.AddressSpaceId);
            
            await _creationSemaphore.WaitAsync(cancellationToken);
            try
            {
                await addressSpaceLock.WaitAsync(cancellationToken);
                try
                {
                    return await CreateIpAllocationWithLockAsync(ipAllocation.AddressSpaceId, ipAllocation.Prefix, ipAllocation.Tags, cancellationToken);
                }
                finally
                {
                    addressSpaceLock.Release();
                }
            }
            finally
            {
                _creationSemaphore.Release();
            }
        }

        private async Task<IpAllocationEntity> CreateIpAllocationWithLockAsync(
            string addressSpaceId,
            string cidr,
            Dictionary<string, string> tags,
            CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    // Validate CIDR format
                    IpamValidator.ValidateCidr(cidr);

                    // Check for existing node with same CIDR
                    var existingNodes = await _ipAllocationRepository.GetByPrefixAsync(addressSpaceId, cidr);
                    var exactMatch = existingNodes.FirstOrDefault(n => n.Prefix == cidr);

                    if (exactMatch != null)
                    {
                        throw new InvalidOperationException($"IP node with CIDR {cidr} already exists in address space {addressSpaceId}");
                    }

                    // Find parent with optimistic concurrency control
                    var parentNode = await FindClosestParentWithVersionCheckAsync(addressSpaceId, cidr);
                    
                    // Apply tag implications with snapshot consistency
                    var effectiveTags = await _tagInheritanceService.ApplyTagImplications(addressSpaceId, tags);

                    // Validate tag inheritance with parent's current state
                    if (parentNode != null)
                    {
                        // Re-fetch parent to ensure we have the latest version
                        var currentParent = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, parentNode.Id);
                        if (currentParent == null)
                        {
                            throw new ConcurrencyException("Parent node was deleted during creation process");
                        }

                        // Validate with current parent state
                        await _tagInheritanceService.ValidateTagInheritance(
                            addressSpaceId, currentParent.Tags, effectiveTags);

                        // Validate same CIDR rule
                        if (currentParent.Prefix == cidr)
                        {
                            await ValidateChildHasAdditionalInheritableTags(addressSpaceId, currentParent.Tags, effectiveTags);
                        }

                        parentNode = currentParent; // Use the current version
                    }

                    // Create the IP node with optimistic concurrency
                    var ipNode = new IpAllocationEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        AddressSpaceId = addressSpaceId,
                        Prefix = cidr,
                        ParentId = parentNode?.Id,
                        Tags = effectiveTags,
                        CreatedOn = DateTime.UtcNow,
                        ModifiedOn = DateTime.UtcNow,
                        ETag = ETag.All // Will be set by Azure Table Storage
                    };

                    // Attempt to create the node
                    var createdNode = await _ipAllocationRepository.CreateAsync(ipNode);

                    // Update parent's children list if parent exists
                    if (parentNode != null)
                    {
                        await UpdateParentChildrenListAsync(parentNode, createdNode.Id, cancellationToken);
                    }

                    return createdNode;
                }
                catch (ConcurrencyException)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw;

                    // Exponential backoff
                    var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount));
                    await Task.Delay(delay, cancellationToken);
                }
                catch (RequestFailedException ex) when (ex.Status == 409) // Conflict
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw new ConcurrencyException("Failed to create IP node due to concurrent modifications", ex);

                    // Exponential backoff
                    var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount));
                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new ConcurrencyException("Maximum retry attempts exceeded for IP node creation");
        }

        private async Task<IpAllocationEntity> FindClosestParentWithVersionCheckAsync(string addressSpaceId, string cidr)
        {
            var targetPrefix = new Prefix(cidr);
            var allNodes = await _ipAllocationRepository.GetAllAsync(addressSpaceId);
            
            IpAllocationEntity closestParent = null;
            int maxMatchingLength = -1;

            foreach (var node in allNodes)
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
                catch (Exception)
                {
                    // Skip invalid prefixes
                    continue;
                }
            }

            return closestParent;
        }

        private async Task UpdateParentChildrenListAsync(IpAllocationEntity parent, string childId, CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    // Re-fetch parent to get current ETag
                    var currentParent = await _ipAllocationRepository.GetByIdAsync(parent.AddressSpaceId, parent.Id);
                    if (currentParent == null)
                    {
                        // Parent was deleted, which is acceptable
                        return;
                    }

                    // Update children list
                    var childrenList = currentParent.ChildrenIds?.ToList() ?? new List<string>();
                    if (!childrenList.Contains(childId))
                    {
                        childrenList.Add(childId);
                        currentParent.ChildrenIds = childrenList;
                        currentParent.ModifiedOn = DateTime.UtcNow;

                        await _ipAllocationRepository.UpdateAsync(currentParent);
                    }
                    return;
                }
                catch (RequestFailedException ex) when (ex.Status == 412) // Precondition Failed (ETag mismatch)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw new ConcurrencyException("Failed to update parent children list due to concurrent modifications", ex);

                    // Short delay before retry
                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        private async Task ValidateChildHasAdditionalInheritableTags(
            string addressSpaceId,
            Dictionary<string, string> parentTags,
            Dictionary<string, string> childTags)
        {
            var parentInheritableCount = 0;
            var childInheritableCount = 0;

            // Count inheritable tags in parent
            if (parentTags != null)
            {
                foreach (var tag in parentTags)
                {
                    var tagDef = await _tagInheritanceService._tagRepository.GetByNameAsync(addressSpaceId, tag.Key);
                    if (tagDef?.Type == "Inheritable")
                        parentInheritableCount++;
                }
            }

            // Count inheritable tags in child
            if (childTags != null)
            {
                foreach (var tag in childTags)
                {
                    var tagDef = await _tagInheritanceService._tagRepository.GetByNameAsync(addressSpaceId, tag.Key);
                    if (tagDef?.Type == "Inheritable")
                        childInheritableCount++;
                }
            }

            if (childInheritableCount <= parentInheritableCount)
            {
                throw new InvalidOperationException(
                    "Child node with same CIDR as parent must have at least one additional inheritable tag");
            }
        }

        private SemaphoreSlim GetAddressSpaceLock(string addressSpaceId)
        {
            lock (_lockDictionary)
            {
                if (!_addressSpaceLocks.TryGetValue(addressSpaceId, out var semaphore))
                {
                    semaphore = new SemaphoreSlim(1, 1); // One concurrent operation per address space
                    _addressSpaceLocks[addressSpaceId] = semaphore;
                }
                return semaphore;
            }
        }

        /// <summary>
        /// Concurrent-safe deletion with proper cleanup
        /// </summary>
        public async Task DeleteIpAllocationAsync(string addressSpaceId, string ipId, CancellationToken cancellationToken = default)
        {
            var addressSpaceLock = GetAddressSpaceLock(addressSpaceId);
            
            await addressSpaceLock.WaitAsync(cancellationToken);
            try
            {
                const int maxRetries = 3;
                var retryCount = 0;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        var nodeToDelete = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, ipId);
                        if (nodeToDelete == null) return;

                        // Get all children
                        var children = await _ipAllocationRepository.GetChildrenAsync(addressSpaceId, ipId);

                        // Propagate inheritable tags to children
                        await _tagInheritanceService.PropagateTagsToChildren(
                            addressSpaceId, nodeToDelete.Tags, children);

                        // Update children to point to the deleted node's parent
                        foreach (var child in children)
                        {
                            child.ParentId = nodeToDelete.ParentId;
                            await _ipAllocationRepository.UpdateAsync(child);
                        }

                        // Remove from parent's children list
                        if (!string.IsNullOrEmpty(nodeToDelete.ParentId))
                        {
                            await RemoveChildFromParentAsync(addressSpaceId, nodeToDelete.ParentId, ipId, cancellationToken);
                        }

                        // Delete the node
                        await _ipAllocationRepository.DeleteAsync(addressSpaceId, ipId);
                        return;
                    }
                    catch (RequestFailedException ex) when (ex.Status == 412 || ex.Status == 409)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                            throw new ConcurrencyException("Failed to delete IP node due to concurrent modifications", ex);

                        await Task.Delay(100 * retryCount, cancellationToken);
                    }
                }
            }
            finally
            {
                addressSpaceLock.Release();
            }
        }

        private async Task RemoveChildFromParentAsync(string addressSpaceId, string parentId, string childId, CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var parent = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, parentId);
                    if (parent != null)
                    {
                        var childrenList = parent.ChildrenIds?.ToList() ?? new List<string>();
                        if (childrenList.Remove(childId))
                        {
                            parent.ChildrenIds = childrenList;
                            parent.ModifiedOn = DateTime.UtcNow;
                            await _ipAllocationRepository.UpdateAsync(parent);
                        }
                    }
                    return;
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        return; // Acceptable to fail parent update in deletion scenario

                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        public void Dispose()
        {
            _creationSemaphore?.Dispose();
            foreach (var semaphore in _addressSpaceLocks.Values)
            {
                semaphore?.Dispose();
            }
        }
    }
}