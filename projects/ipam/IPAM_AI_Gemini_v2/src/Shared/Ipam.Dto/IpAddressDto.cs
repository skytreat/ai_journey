
using System;
using System.Collections.Generic;

namespace Ipam.Dto
{
    public class IpAddressDto
    {
        public Guid Id { get; set; }
        public Guid AddressSpaceId { get; set; }
        public string Prefix { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public Guid? ParentId { get; set; }
        public List<Guid> ChildrenIds { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
    }
}
