
using System;
using System.Collections.Generic;

namespace Ipam.Core
{
    public class IpAddress
    {
        public IpAddress()
        {
            Prefix = string.Empty;
            Tags = new Dictionary<string, string>();
            ChildrenIds = new List<Guid>();
            InheritedTags = new Dictionary<string, string>();
        }

        public Guid Id { get; set; }
        public Guid AddressSpaceId { get; set; }
        public string Prefix { get; set; }
        // Direct tags set on this IP
        public Dictionary<string, string> Tags { get; set; }
        // Tags inherited from parent nodes
        public Dictionary<string, string> InheritedTags { get; set; }
        public Guid? ParentId { get; set; }
        public List<Guid> ChildrenIds { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
    }
}
