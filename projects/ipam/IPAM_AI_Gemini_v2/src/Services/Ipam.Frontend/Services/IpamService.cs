
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ipam.Core;
using Ipam.DataAccess.Interfaces;

namespace Ipam.Frontend.Services
{
    public class IpamService : IIpamService
    {
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ITagRepository _tagRepository;        
        public IpamService(IIpAddressRepository ipAddressRepository, ITagRepository tagRepository)
        {
            _ipAddressRepository = ipAddressRepository;
            _tagRepository = tagRepository;
        }

        public async Task<IpAddress> CreateIpAddressAsync(IpAddress ipAddress)
        {            
            await FindAndSetParentIpAsync(ipAddress);
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

        private async Task FindAndSetParentIpAsync(IpAddress ipAddress)
        {
            if (ipAddress.ParentId.HasValue)
            {
                return; // Parent is already specified by the user.
            }

            var allIps = await _ipAddressRepository.GetIpAddressesAsync(ipAddress.AddressSpaceId, null, null);
            IpNetwork childNetwork;
            try
            {
                childNetwork = new IpNetwork(ipAddress.Prefix);
            }
            catch (Exception)
            {
                // Invalid CIDR on the new IP. Let the validation handle it.
                return;
            }

            IpAddress bestParent = null;
            int bestParentCidr = -1;

            foreach (var potentialParent in allIps)
            {
                // An IP cannot be its own parent.
                if (potentialParent.Id == ipAddress.Id) continue;

                IpNetwork parentNetwork;
                try
                {
                    parentNetwork = new IpNetwork(potentialParent.Prefix);
                }
                catch (Exception)
                {
                    // Skip invalid CIDR entries in the database.
                    continue;
                }

                // Check if the potential parent's network contains the new IP's network.
                if (parentNetwork.Contains(childNetwork))
                {
                    // We are looking for the "closest" parent, which means the one with the
                    // largest CIDR prefix (e.g., /24 is closer than /16).
                    if (parentNetwork.Cidr > bestParentCidr)
                    {
                        bestParent = potentialParent;
                        bestParentCidr = parentNetwork.Cidr;
                    }
                }
            }

            if (bestParent != null)
            {
                ipAddress.ParentId = bestParent.Id;
            }
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
                if (parentIpAddress == null)
                {
                    // This check is crucial. If a parent is explicitly specified, it MUST exist.
                    // This prevents race conditions where a child is created for a parent that doesn't exist yet.
                    throw new InvalidOperationException($"The specified ParentId '{ipAddress.ParentId.Value}' does not correspond to an existing IP address.");
                }
                
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

        private async Task<Dictionary<string, string>> ApplyTagImplications(Guid addressSpaceId, Dictionary<string, string> tags)
        {
            var impliedTags = new Dictionary<string, string>(tags);
            var implications = await _tagRepository.GetTagImplicationsAsync(addressSpaceId);

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

    /// <summary>
    /// A helper class for handling IPv4 network calculations. It is file-private.
    /// </summary>
    file class IpNetwork
    {
        private readonly uint _networkAddress;
        private readonly uint _broadcastAddress;
        public int Cidr { get; }

        public IpNetwork(string cidrString)
        {
            var parts = cidrString.Split('/');
            if (parts.Length != 2) throw new ArgumentException("Invalid CIDR format.");

            if (!IPAddress.TryParse(parts[0], out var ip)) throw new ArgumentException("Invalid IP address.");
            if (!int.TryParse(parts[1], out var cidr) || cidr < 0 || cidr > 32) throw new ArgumentException("Invalid CIDR prefix.");

            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                throw new NotSupportedException("Only IPv4 is supported for automatic parent finding.");
            }

            Cidr = cidr;
            var ipBytes = ip.GetAddressBytes();
            // Convert big-endian byte array to a uint for calculation
            uint ipUint = ((uint)ipBytes[0] << 24) | ((uint)ipBytes[1] << 16) | ((uint)ipBytes[2] << 8) | ipBytes[3];

            // Create mask
            uint mask = cidr == 0 ? 0 : 0xFFFFFFFF << (32 - Cidr);

            _networkAddress = ipUint & mask;
            _broadcastAddress = _networkAddress | ~mask;
        }

        public bool Contains(IpNetwork other)
        {
            // A network contains another if:
            // 1. Its prefix is smaller or equal (e.g., /8 contains /16).
            // 2. The other network's address range is entirely within this network's range.
            // 3. It is not the same network.
            if (this._networkAddress == other._networkAddress && this.Cidr == other.Cidr)
            {
                return false;
            }
            return this.Cidr <= other.Cidr &&
                   this._networkAddress <= other._networkAddress &&
                   this._broadcastAddress >= other._broadcastAddress;
        }
    }
}
