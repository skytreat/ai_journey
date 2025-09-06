
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Core;

namespace Ipam.DataAccess.Interfaces
{
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetRolesAsync(string username);
        Task<Role> CreateRoleAsync(Role role);
        Task DeleteRoleAsync(string username, Guid addressSpaceId);
    }
}
