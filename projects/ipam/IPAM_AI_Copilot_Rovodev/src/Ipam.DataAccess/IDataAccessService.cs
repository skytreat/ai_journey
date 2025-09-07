using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.DataAccess.Models;

namespace Ipam.DataAccess
{
    public interface IDataAccessService
    {
        Task<IpAllocation> CreateIPAddressAsync(IpAllocation ipAllocation);
        Task<IpAllocation> GetIPAddressAsync(string addressSpaceId, string ipId);
        Task<IEnumerable<IpAllocation>> GetIPAddressesAsync(string addressSpaceId, string cidr = null, Dictionary<string, string> tags = null);
        Task<IpAllocation> UpdateIPAddressAsync(IpAllocation ipAllocation);
        Task DeleteIPAddressAsync(string addressSpaceId, string ipId);
        Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace);
        Task<AddressSpace> GetAddressSpaceAsync(string addressSpaceId);
        Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync();
        Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace);
        Task DeleteAddressSpaceAsync(string addressSpaceId);
        Task<Tag> CreateTagAsync(string addressSpaceId, Tag tag);
        Task<Tag> GetTagAsync(string addressSpaceId, string tagName);
        Task<IEnumerable<Tag>> GetTagsAsync(string addressSpaceId);
        Task<Tag> UpdateTagAsync(string addressSpaceId, Tag tag);
        Task DeleteTagAsync(string addressSpaceId, string tagName);
    }
}