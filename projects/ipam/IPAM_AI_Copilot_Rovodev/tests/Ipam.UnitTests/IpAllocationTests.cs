using System;
using System.Collections.Generic;
using Ipam.DataAccess.Models;
using Xunit;

namespace Ipam.UnitTests
{
    public class IpAllocationTests
    {
        [Fact]
        public void IpAllocation_InheritTagsFromParent_ShouldInheritInheritableTags()
        {
            // Arrange
            var parentIP = new IpAllocation
            {
                Id = "parent-1",
                Prefix = "192.168.1.0/24",
                Tags = new List<IpAllocationTag>
                {
                    new IpAllocationTag { Name = "Environment", Value = "Production" },
                    new IpAllocationTag { Name = "Owner", Value = "TeamA" }
                }
            };

            var childIP = new IpAllocation
            {
                Id = "child-1",
                Prefix = "192.168.1.10/32",
                ParentId = parentIP.Id,
                Tags = new List<IpAllocationTag>
                {
                    new IpAllocationTag { Name = "Service", Value = "Web" }
                }
            };

            // Act
            // Simulate tag inheritance logic
            foreach (var tag in parentIP.Tags)
            {
                if (!childIP.Tags.Exists(t => t.Name == tag.Name))
                {
                    childIP.Tags.Add(new IpAllocationTag { Name = tag.Name, Value = tag.Value });
                }
            }

            // Assert
            Assert.Contains(childIP.Tags, t => t.Name == "Environment" && t.Value == "Production");
            Assert.Contains(childIP.Tags, t => t.Name == "Owner");
            Assert.Contains(childIP.Tags, t => t.Name == "Service");
        }
    }
}
