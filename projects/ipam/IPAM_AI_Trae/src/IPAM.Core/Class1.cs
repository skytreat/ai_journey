using System;
using System.Collections.Generic;

namespace IPAM.Core
{
    public class AddressSpace
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

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
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public TagType Type { get; set; }
        public Dictionary<string, string> KnownValues { get; set; }
        public Dictionary<string, Dictionary<string, string>> Implies { get; set; }
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; }
    }

    public class IP
    {
        public Guid AddressSpaceId { get; set; }
        public Guid Id { get; set; }
        public string Prefix { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public Guid? ParentId { get; set; }
        public List<Guid> ChildrenIds { get; set; }
    }
}