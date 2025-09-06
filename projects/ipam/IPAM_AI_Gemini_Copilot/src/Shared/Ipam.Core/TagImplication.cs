
using System;

namespace Ipam.Core
{
    public class TagImplication
    {
        public Guid AddressSpaceId { get; set; }
        public string IfTagValue { get; set; }
        public string ThenTagValue { get; set; }
    }
}
