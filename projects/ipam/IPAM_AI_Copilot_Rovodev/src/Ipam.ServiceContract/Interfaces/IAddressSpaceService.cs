using Ipam.ServiceContract.DTOs;

namespace Ipam.ServiceContract.Interfaces;

public interface IAddressSpaceService
{
    Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace, CancellationToken cancellationToken = default);
    Task<AddressSpace?> GetAddressSpaceByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync(CancellationToken cancellationToken = default);
    Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace, CancellationToken cancellationToken = default);
    Task DeleteAddressSpaceAsync(string id, CancellationToken cancellationToken = default);
}