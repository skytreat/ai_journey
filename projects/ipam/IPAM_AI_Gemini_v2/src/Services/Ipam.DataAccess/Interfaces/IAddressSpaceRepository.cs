
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Core;

namespace Ipam.DataAccess.Interfaces
{
    public interface IAddressSpaceRepository
    {
        Task<AddressSpace> GetAddressSpaceAsync(Guid id);
        Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync();
        Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace);
        Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace);
        Task DeleteAddressSpaceAsync(Guid id);
    }
}
