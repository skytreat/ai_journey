using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Service for handling tag inheritance and implication logic
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagInheritanceService
    {
        internal readonly ITagRepository _tagRepository;

        public TagInheritanceService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        /// <summary>
        /// Applies tag implications based on the tag rules
        /// Example: Datacenter=AMS05 implies Region=EuropeWest
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="inputTags">The input tags to process</param>
        /// <returns>Tags with implications applied</returns>
        public async Task<Dictionary<string, string>> ApplyTagImplications(
            string addressSpaceId, 
            Dictionary<string, string> inputTags)
        {
            var resultTags = new Dictionary<string, string>(inputTags);
            var processedTags = new HashSet<string>();

            // Keep applying implications until no new tags are added
            bool hasChanges;
            do
            {
                hasChanges = false;
                var currentTags = new Dictionary<string, string>(resultTags);

                foreach (var tag in currentTags)
                {
                    if (processedTags.Contains($"{tag.Key}={tag.Value}"))
                        continue;

                    var tagDefinition = await _tagRepository.GetByNameAsync(addressSpaceId, tag.Key);
                    if (tagDefinition?.Type == "Inheritable" && tagDefinition.Implies != null)
                    {
                        // The Implies dictionary on a tag definition contains the tags that *it* implies.
                        // The key is the implied tag's name.
                        foreach (var implication in tagDefinition.Implies)
                        {
                            var impliedTagName = implication.Key;
                            var valueMappings = implication.Value; // This maps current tag's value to implied tag's value.

                            if (valueMappings.TryGetValue(tag.Value, out var impliedTagValue))
                            {
                                if (!resultTags.ContainsKey(impliedTagName))
                                {
                                    resultTags[impliedTagName] = impliedTagValue;
                                    hasChanges = true;
                                }
                                else if (resultTags[impliedTagName] != impliedTagValue)
                                {
                                    throw new InvalidOperationException(
                                        $"Tag implication conflict: {impliedTagName} already has value {resultTags[impliedTagName]}, " +
                                        $"but {tag.Key}={tag.Value} implies {impliedTagName}={impliedTagValue}");
                                }
                            }
                        }
                    }

                    processedTags.Add($"{tag.Key}={tag.Value}");
                }
            } while (hasChanges);

            return resultTags;
        }

        /// <summary>
        /// Validates that child tags don't conflict with inheritable parent tags
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="parentTags">Parent node tags</param>
        /// <param name="childTags">Child node tags</param>
        public async Task ValidateTagInheritance(
            string addressSpaceId,
            Dictionary<string, string> parentTags,
            Dictionary<string, string> childTags)
        {
            if (parentTags == null || childTags == null) return;

            foreach (var parentTag in parentTags)
            {
                var tagDefinition = await _tagRepository.GetByNameAsync(addressSpaceId, parentTag.Key);
                
                // Only check inheritable tags
                if (tagDefinition?.Type == "Inheritable")
                {
                    if (childTags.TryGetValue(parentTag.Key, out string childValue) && 
                        childValue != parentTag.Value)
                    {
                        throw new InvalidOperationException(
                            $"Inheritable tag conflict: Parent has {parentTag.Key}={parentTag.Value}, " +
                            $"but child has {parentTag.Key}={childValue}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets effective tags for a node (including inherited tags from parents)
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="nodeTags">The node's own tags</param>
        /// <param name="parentTags">The parent's tags</param>
        /// <returns>Combined effective tags</returns>
        public async Task<Dictionary<string, string>> GetEffectiveTags(
            string addressSpaceId,
            Dictionary<string, string> nodeTags,
            Dictionary<string, string> parentTags)
        {
            var effectiveTags = new Dictionary<string, string>();

            // Add inheritable parent tags first
            if (parentTags != null)
            {
                foreach (var parentTag in parentTags)
                {
                    var tagDefinition = await _tagRepository.GetByNameAsync(addressSpaceId, parentTag.Key);
                    if (tagDefinition?.Type == "Inheritable")
                    {
                        effectiveTags[parentTag.Key] = parentTag.Value;
                    }
                }
            }

            // Override with node's own tags
            if (nodeTags != null)
            {
                foreach (var nodeTag in nodeTags)
                {
                    effectiveTags[nodeTag.Key] = nodeTag.Value;
                }
            }

            return effectiveTags;
        }

        /// <summary>
        /// Propagates inheritable tags from parent to children when parent is deleted
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <param name="parentTags">Tags from the deleted parent</param>
        /// <param name="childNodes">Child nodes to update</param>
        public async Task PropagateTagsToChildren(
            string addressSpaceId,
            Dictionary<string, string> parentTags,
            IEnumerable<IpAllocationEntity> childNodes)
        {
            if (parentTags == null || !childNodes.Any()) return;

            var inheritableTags = new Dictionary<string, string>();

            // Filter inheritable tags
            foreach (var tag in parentTags)
            {
                var tagDefinition = await _tagRepository.GetByNameAsync(addressSpaceId, tag.Key);
                if (tagDefinition?.Type == "Inheritable")
                {
                    inheritableTags[tag.Key] = tag.Value;
                }
            }

            // Add inheritable tags to children if they don't already have them
            foreach (var child in childNodes)
            {
                foreach (var inheritableTag in inheritableTags)
                {
                    if (!child.Tags.ContainsKey(inheritableTag.Key))
                    {
                        child.Tags[inheritableTag.Key] = inheritableTag.Value;
                    }
                }
            }
        }
    }
}