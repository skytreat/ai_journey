using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IPAM.Core;

namespace IPAM.Data
{
    public interface IRepository
    {
        // AddressSpace 相关操作
        Task<AddressSpace> GetAddressSpaceById(Guid id);
        Task<List<AddressSpace>> GetAddressSpaces(string keyword = null, DateTime? createdAfter = null, DateTime? createdBefore = null);
        Task AddAddressSpace(AddressSpace addressSpace);
        Task UpdateAddressSpace(AddressSpace addressSpace);
        Task DeleteAddressSpace(Guid id);

        // Tag 相关操作
        Task<Tag> GetTagById(Guid addressSpaceId, string name);
        Task<List<Tag>> GetTags(Guid addressSpaceId, string keyword = null);
        Task AddTag(Tag tag);
        Task UpdateTag(Tag tag);
        Task DeleteTag(Guid addressSpaceId, string name);

        // IP 相关操作
        Task<IP> GetIpById(Guid addressSpaceId, Guid id);
        Task<IP> GetIpByPrefix(Guid addressSpaceId, string prefix);
        Task<List<IP>> GetIpsByTags(Guid addressSpaceId, Dictionary<string, string> tags);
        Task<List<IP>> GetChildIps(Guid addressSpaceId, Guid parentId);
        Task AddIp(IP ip);
        Task UpdateIp(IP ip);
        Task DeleteIp(Guid addressSpaceId, Guid id);
    }
}