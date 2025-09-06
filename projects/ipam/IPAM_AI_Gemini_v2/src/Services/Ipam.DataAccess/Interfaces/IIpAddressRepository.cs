
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Core;

namespace Ipam.DataAccess.Interfaces
{
    public interface IIpAddressRepository
    {
        Task<IpAddress> GetIpAddressAsync(Guid addressSpaceId, Guid id);
        Task<IEnumerable<IpAddress>> GetIpAddressesAsync(Guid addressSpaceId, string cidr, Dictionary<string, string> tags);
        Task<IpAddress> CreateIpAddressAsync(IpAddress ipAddress);
        Task<IpAddress> UpdateIpAddressAsync(IpAddress ipAddress);
        Task DeleteIpAddressAsync(Guid addressSpaceId, Guid id);
        Task<IEnumerable<IpAddress>> GetChildrenAsync(Guid addressSpaceId, Guid id);
    }
}
