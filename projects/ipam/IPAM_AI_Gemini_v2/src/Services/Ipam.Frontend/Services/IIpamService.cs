
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Core;

namespace Ipam.Frontend.Services
{
    public interface IIpamService
    {
        Task<IpAddress?> GetIpAddressWithInheritedTagsAsync(Guid addressSpaceId, Guid id);
        Task<IpAddress> CreateIpAddressAsync(IpAddress ipAddress);
        Task<IpAddress> UpdateIpAddressAsync(IpAddress ipAddress);
        Task DeleteIpAddressAsync(Guid addressSpaceId, Guid id);
    }
}
