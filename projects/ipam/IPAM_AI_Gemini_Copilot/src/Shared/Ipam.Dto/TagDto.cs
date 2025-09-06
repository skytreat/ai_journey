
using System;
using System.Collections.Generic;

namespace Ipam.Dto
{
    public enum TagType
    {
        Inheritable,
        NonInheritable
    }

    public class TagDto
    {
        public TagDto()
        {
            Name = string.Empty;
            Description = string.Empty;
            KnownValues = new List<string>();
            Attributes = new Dictionary<string, Dictionary<string, string>>();
            Implies = new Dictionary<string, Dictionary<string, string>>();
        }

        public Guid AddressSpaceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TagType Type { get; set; }
        public List<string> KnownValues { get; set; }
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; }
        // Implies is a dictionary where:
        // - Key is the tag name to be implied
        // - Value is a dictionary where:
        //   - Key is the current tag's value
        //   - Value is the implied tag's value
        public Dictionary<string, Dictionary<string, string>> Implies { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
    }
}
