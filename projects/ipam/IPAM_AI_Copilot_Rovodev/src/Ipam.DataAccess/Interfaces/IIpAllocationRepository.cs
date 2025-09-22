using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for IP node repository operations
    /// </summary>
    public interface IIpAllocationRepository
    {
        Task<IpAllocationEntity> GetByIdAsync(string addressSpaceId, string ipId);

        Task<IList<IpAllocationEntity>> GetAllAsync(string addressSpaceId);

        Task<IEnumerable<IpAllocationEntity>> GetByPrefixAsync(string addressSpaceId, string cidr);
        Task<IEnumerable<IpAllocationEntity>> GetByTagsAsync(string addressSpaceId, Dictionary<string, string> tags);
        Task<IEnumerable<IpAllocationEntity>> GetChildrenAsync(string addressSpaceId, string parentId);
        Task<IpAllocationEntity> CreateAsync(IpAllocationEntity ipNode);
        Task<IpAllocationEntity> UpdateAsync(IpAllocationEntity ipNode);
        Task DeleteAsync(string addressSpaceId, string ipId);
    }
}