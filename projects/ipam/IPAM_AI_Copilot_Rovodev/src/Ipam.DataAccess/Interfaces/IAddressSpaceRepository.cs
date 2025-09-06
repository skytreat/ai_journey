using Ipam.DataAccess.Models;
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
        Task<AddressSpace> GetByIdAsync(string partitionId, string addressSpaceId);
        Task<IEnumerable<AddressSpace>> QueryAsync(string nameFilter = null, DateTime? createdAfter = null);
        Task<AddressSpace> CreateAsync(AddressSpace addressSpace);
        Task<AddressSpace> UpdateAsync(AddressSpace addressSpace);
        Task DeleteAsync(string partitionId, string addressSpaceId);
    }
}
