
using System;
using Xunit;

namespace Ipam.Core.Tests
{
    public class AddressSpaceTests
    {
        [Fact]
        public void AddressSpace_CanBeCreated()
        {
            var id = Guid.NewGuid();
            var name = "TestSpace";
            var description = "A test address space";
            var createdOn = DateTimeOffset.UtcNow;
            var modifiedOn = DateTimeOffset.UtcNow;

            var addressSpace = new AddressSpace
            {
                Id = id,
                Name = name,
                Description = description,
                CreatedOn = createdOn,
                ModifiedOn = modifiedOn
            };

            Assert.Equal(id, addressSpace.Id);
            Assert.Equal(name, addressSpace.Name);
            Assert.Equal(description, addressSpace.Description);
            Assert.Equal(createdOn, addressSpace.CreatedOn);
            Assert.Equal(modifiedOn, addressSpace.ModifiedOn);
        }
    }
}
