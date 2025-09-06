
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipam.Core;
using Ipam.DataAccess.Interfaces;

namespace Ipam.Frontend.Services
{
    public class IpamService : IIpamService
    {
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ITagImplicationRepository _tagImplicationRepository;

        public IpamService(IIpAddressRepository ipAddressRepository, ITagRepository tagRepository, ITagImplicationRepository tagImplicationRepository)
        {
            _ipAddressRepository = ipAddressRepository;
            _tagRepository = tagRepository;
            _tagImplicationRepository = tagImplicationRepository;
        }

        public async Task<IpAddress> CreateIpAddressAsync(IpAddress ipAddress)
        {
            await ValidateAndApplyTags(ipAddress);
            return await _ipAddressRepository.CreateIpAddressAsync(ipAddress);
        }

        public async Task<IpAddress> UpdateIpAddressAsync(IpAddress ipAddress)
        {
            await ValidateAndApplyTags(ipAddress);
            return await _ipAddressRepository.UpdateIpAddressAsync(ipAddress);
        }

        public async Task DeleteIpAddressAsync(Guid addressSpaceId, Guid id)
        {
            var ipAddress = await _ipAddressRepository.GetIpAddressAsync(addressSpaceId, id);
            if (ipAddress == null)
            {
                return;
            }

            var children = await _ipAddressRepository.GetChildrenAsync(addressSpaceId, id);
            if (children.Any())
            {
                var parentTags = await GetIpAddressWithInheritedTagsAsync(addressSpaceId, id);
                if (parentTags != null)
                {
                    foreach (var child in children)
                    {
                        child.ParentId = ipAddress.ParentId;
                        // Push down inheritable tags
                        foreach (var tag in parentTags.Tags)
                        {
                            var tagDef = await _tagRepository.GetTagAsync(addressSpaceId, tag.Key);
                            if (tagDef != null && tagDef.Type == TagType.Inheritable && !child.Tags.ContainsKey(tag.Key))
                            {
                                child.Tags.Add(tag.Key, tag.Value);
                            }
                        }
                    }
                }
            }

            await _ipAddressRepository.DeleteIpAddressAsync(addressSpaceId, id);
        }

        public async Task<IpAddress?> GetIpAddressWithInheritedTagsAsync(Guid addressSpaceId, Guid id)
        {
            var ipAddress = await _ipAddressRepository.GetIpAddressAsync(addressSpaceId, id);
            if (ipAddress == null)
            {
                return null;
            }

            var allTags = new Dictionary<string, string>(ipAddress.Tags);
            var current = ipAddress;
            while (current.ParentId.HasValue)
            {
                current = await _ipAddressRepository.GetIpAddressAsync(addressSpaceId, current.ParentId.Value);
                if (current == null)
                {
                    break;
                }

                var parentTags = await _tagRepository.GetTagsAsync(addressSpaceId);
                foreach (var tag in current.Tags)
                {
                    var tagDef = parentTags.FirstOrDefault(t => t.Name == tag.Key);
                    if (tagDef != null && tagDef.Type == TagType.Inheritable && !allTags.ContainsKey(tag.Key))
                    {
                        allTags.Add(tag.Key, tag.Value);
                    }
                }
            }

            ipAddress.Tags = allTags;
            return ipAddress;
        }

        private async Task ValidateAndApplyTags(IpAddress ipAddress)
        {
            // 1. Apply tag implications
            ipAddress.Tags = await ApplyTagImplications(ipAddress.AddressSpaceId, ipAddress.Tags);

            // 2. Validate KnownValues
            var allTagDefinitions = (await _tagRepository.GetTagsAsync(ipAddress.AddressSpaceId)).ToList();
            foreach (var tag in ipAddress.Tags)
            {
                var tagDef = allTagDefinitions.FirstOrDefault(t => t.Name == tag.Key);
                if (tagDef != null && tagDef.KnownValues != null && tagDef.KnownValues.Any() && !tagDef.KnownValues.Contains(tag.Value))
                {
                    throw new InvalidOperationException($"Tag '{tag.Key}' has a value '{tag.Value}' that is not in its KnownValues list.");
                }
            }

            // 3. Check for conflicting inheritable tags with parent
            if (ipAddress.ParentId.HasValue)
            {
                var parentIpAddress = await GetIpAddressWithInheritedTagsAsync(ipAddress.AddressSpaceId, ipAddress.ParentId.Value);
                if (parentIpAddress != null)
                {
                    var currentIpInheritableTags = ipAddress.Tags.Where(t => allTagDefinitions.Any(td => td.Name == t.Key && td.Type == TagType.Inheritable)).ToDictionary(t => t.Key, t => t.Value);
                    var parentIpInheritableTags = parentIpAddress.Tags.Where(t => allTagDefinitions.Any(td => td.Name == t.Key && td.Type == TagType.Inheritable)).ToDictionary(t => t.Key, t => t.Value);

                    foreach (var parentTag in parentIpInheritableTags)
                    {
                        if (currentIpInheritableTags.ContainsKey(parentTag.Key) && currentIpInheritableTags[parentTag.Key] != parentTag.Value)
                        {
                            throw new InvalidOperationException($"Conflicting inheritable tag '{parentTag.Key}' with parent. Parent has '{parentTag.Value}', child has '{currentIpInheritableTags[parentTag.Key]}'.");
                        }
                    }

                    // 4. IP node rule: same CIDR requires more inheritable tags
                    if (ipAddress.Prefix == parentIpAddress.Prefix)
                    {
                        var inheritedTagsCount = parentIpInheritableTags.Count;
                        var currentTagsCount = currentIpInheritableTags.Count;

                        if (currentTagsCount <= inheritedTagsCount)
                        {
                            throw new InvalidOperationException("Child IP with same CIDR as parent must have at least one more inheritable tag.");
                        }
                    }
                }
            }
        }

        private async Task<Dictionary<string, string>> ApplyTagImplications(Guid addressSpaceId, Dictionary<string, string> tags)
        {
            var impliedTags = new Dictionary<string, string>(tags);
            var implications = await _tagImplicationRepository.GetTagImplicationsAsync(addressSpaceId);

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var implication in implications)
                {
                    var ifTag = implication.IfTagValue.Split(':');
                    var thenTag = implication.ThenTagValue.Split(':');

                    if (impliedTags.ContainsKey(ifTag[0]) && impliedTags[ifTag[0]] == ifTag[1])
                    {
                        if (!impliedTags.ContainsKey(thenTag[0]))
                        {
                            impliedTags.Add(thenTag[0], thenTag[1]);
                            changed = true;
                        } else if (impliedTags[thenTag[0]] != thenTag[1])
                        {
                            // Conflict, handle as per requirements (e.g., throw error or override)
                            // For now, we'll just override
                            impliedTags[thenTag[0]] = thenTag[1];
                            changed = true;
                        }
                    }
                }
            }
            return impliedTags;
        }
    }
}
