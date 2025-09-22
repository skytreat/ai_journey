using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Validation;
using Ipam.ServiceContract.DTOs;
using Ipam.ServiceContract.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Service for managing IP tree structure and relationships
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpTreeService
    {
        private readonly IIpAllocationRepository _ipNodeRepository;
        private readonly TagInheritanceService _tagInheritanceService;

        public IpTreeService(
            IIpAllocationRepository ipNodeRepository,
            TagInheritanceService tagInheritanceService)
        {
            _ipNodeRepository = ipNodeRepository;
            _tagInheritanceService = tagInheritanceService;
        }

        /// <summary>
        /// Creates an IP node with automatic parent detection
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="cidr">The CIDR notation</param>
        /// <param name="tags">The tags for the IP node</param>
        /// <returns>The created IP node</returns>
        public async Task<IpAllocationEntity> CreateIpAllocationAsync(
            string addressSpaceId, 
            string cidr, 
            Dictionary<string, string> tags)
        {
            // Validate CIDR format
            IpamValidator.ValidateCidr(cidr);

            // Find the closest parent node
            var parentNode = await FindClosestParentAsync(addressSpaceId, cidr);

            // Apply tag implications
            var effectiveTags = await _tagInheritanceService.ApplyTagImplications(addressSpaceId, tags);

            // Validate tag inheritance if parent exists
            if (parentNode != null)
            {
                var parentEffectiveTags = await _tagInheritanceService.GetEffectiveTags(
                    addressSpaceId, parentNode.Tags, null);
                
                await _tagInheritanceService.ValidateTagInheritance(
                    addressSpaceId, parentEffectiveTags, effectiveTags);
            }

            // Validate that child has at least one more inheritable tag than parent if CIDR is same
            if (parentNode != null && parentNode.Prefix == cidr)
            {
                await ValidateChildHasAdditionalInheritableTags(addressSpaceId, parentNode.Tags, effectiveTags);
            }

            // Create the IP node
            var ipNode = new IpAllocationEntity
            {
                Id = Guid.NewGuid().ToString(),
                AddressSpaceId = addressSpaceId,
                Prefix = cidr,
                ParentId = parentNode?.Id,
                Tags = effectiveTags,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            // Create the node
            var createdNode = await _ipNodeRepository.CreateAsync(ipNode);

            // Update parent's children list
            if (parentNode != null)
            {
                await AddChildToParent(parentNode, createdNode.Id);
            }

            return createdNode;
        }

        /// <summary>
        /// Finds the closest parent node for a given CIDR
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="cidr">The CIDR to find parent for</param>
        /// <returns>The closest parent node or null if no parent found</returns>
        public async Task<IpAllocationEntity> FindClosestParentAsync(string addressSpaceId, string cidr)
        {
            var targetPrefix = new Prefix(cidr);
            var allNodes = await _ipNodeRepository.GetChildrenAsync(addressSpaceId, null);
            
            IpAllocationEntity closestParent = null;
            int maxMatchingLength = -1;

            foreach (var node in allNodes)
            {
                try
                {
                    var nodePrefix = new Prefix(node.Prefix);
                    
                    // Check if this node is a supernet of the target
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

        /// <summary>
        /// Deletes an IP node and handles child node updates
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="ipId">The IP node ID to delete</param>
        public async Task DeleteIpAllocationAsync(string addressSpaceId, string ipId)
        {
            var nodeToDelete = await _ipNodeRepository.GetByIdAsync(addressSpaceId, ipId);
            if (nodeToDelete == null) return;

            // Get all children
            var children = await _ipNodeRepository.GetChildrenAsync(addressSpaceId, ipId);

            // Propagate inheritable tags to children
            await _tagInheritanceService.PropagateTagsToChildren(
                addressSpaceId, nodeToDelete.Tags, children);

            // Update children to point to the deleted node's parent
            foreach (var child in children)
            {
                child.ParentId = nodeToDelete.ParentId;
                await _ipNodeRepository.UpdateAsync(child);
            }

            // Remove from parent's children list
            if (!string.IsNullOrEmpty(nodeToDelete.ParentId))
            {
                await RemoveChildFromParent(nodeToDelete.ParentId, ipId);
            }

            // Delete the node
            await _ipNodeRepository.DeleteAsync(addressSpaceId, ipId);
        }

        /// <summary>
        /// Gets all child nodes for a given parent
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="parentId">The parent node ID</param>
        /// <returns>List of child nodes</returns>
        public async Task<IEnumerable<IpAllocationEntity>> GetChildrenAsync(string addressSpaceId, string parentId)
        {
            return await _ipNodeRepository.GetChildrenAsync(addressSpaceId, parentId);
        }

        /// <summary>
        /// Validates that child has at least one more inheritable tag than parent when CIDR is same
        /// </summary>
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

        /// <summary>
        /// Adds a child ID to parent's children list
        /// </summary>
        private async Task AddChildToParent(IpAllocationEntity parent, string childId)
        {
            var childrenList = parent.ChildrenIds?.ToList() ?? new List<string>();
            if (!childrenList.Contains(childId))
            {
                childrenList.Add(childId);
                parent.ChildrenIds = childrenList;
                await _ipNodeRepository.UpdateAsync(parent);
            }
        }

        /// <summary>
        /// Removes a child ID from parent's children list
        /// </summary>
        private async Task RemoveChildFromParent(string parentId, string childId)
        {
            var parent = await _ipNodeRepository.GetByIdAsync(parentId.Split('/')[0], parentId);
            if (parent != null)
            {
                var childrenList = parent.ChildrenIds?.ToList() ?? new List<string>();
                if (childrenList.Remove(childId))
                {
                    parent.ChildrenIds = childrenList;
                    await _ipNodeRepository.UpdateAsync(parent);
                }
            }
        }
    }
}