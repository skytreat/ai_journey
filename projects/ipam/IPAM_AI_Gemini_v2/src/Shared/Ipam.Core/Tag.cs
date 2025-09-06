
using System;
using System.Collections.Generic;

namespace Ipam.Core
{
    public enum TagType
    {
        Inheritable,
        NonInheritable
    }

    public class Tag
    {
        public Guid AddressSpaceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TagType Type { get; set; }
        public List<string> KnownValues { get; set; }
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
    }
}
