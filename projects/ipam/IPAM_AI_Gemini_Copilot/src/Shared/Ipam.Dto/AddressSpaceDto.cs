
using System;

namespace Ipam.Dto
{
    public class AddressSpaceDto
    {
        public AddressSpaceDto()
        {
            Name = string.Empty;
            Description = string.Empty;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
    }
}
