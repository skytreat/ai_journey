
using System;

namespace Ipam.Dto
{
    public class RoleDto
    {
        public string Username { get; set; }
        public Guid AddressSpaceId { get; set; }
        public string Role { get; set; }
    }
}
