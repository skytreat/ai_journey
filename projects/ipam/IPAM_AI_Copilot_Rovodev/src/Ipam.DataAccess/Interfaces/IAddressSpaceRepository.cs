using Ipam.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for address space repository operations
    /// </summary>
    public interface IAddressSpaceRepository
    {
        Task<AddressSpaceEntity> GetByIdAsync(string partitionId, string addressSpaceId);
        Task<IEnumerable<AddressSpaceEntity>> QueryAsync(string nameFilter = null, DateTime? createdAfter = null);
        Task<IEnumerable<AddressSpaceEntity>> GetAllAsync(string partitionId);
        Task<AddressSpaceEntity> CreateAsync(AddressSpaceEntity addressSpace);
        Task<AddressSpaceEntity> UpdateAsync(AddressSpaceEntity addressSpace);
        Task DeleteAsync(string partitionId, string addressSpaceId);
    }
}