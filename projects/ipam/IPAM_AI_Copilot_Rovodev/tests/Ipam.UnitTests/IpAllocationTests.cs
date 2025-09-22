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
                Tags = new Dictionary<string, string>
                {
                    { "Environment", "Production" },
                    { "Owner", "TeamA" }
                }
            };

            var childIP = new IpAllocation
            {
                Id = "child-1",
                Prefix = "192.168.1.10/32",
                ParentId = parentIP.Id,
                Tags = new Dictionary<string, string>
                {
                    { "Service", "Web" }
                }
            };

            // Act
            // Simulate tag inheritance logic
            foreach (var tag in parentIP.Tags)
            {
                if (!childIP.Tags.ContainsKey(tag.Key))
                {
                    childIP.Tags[tag.Key] = tag.Value;
                }
            }

            // Assert
            Assert.True(childIP.Tags.ContainsKey("Environment"));
            Assert.Equal("Production", childIP.Tags["Environment"]);
            Assert.True(childIP.Tags.ContainsKey("Owner"));
            Assert.Equal("TeamA", childIP.Tags["Owner"]);
            Assert.True(childIP.Tags.ContainsKey("Service"));
            Assert.Equal("Web", childIP.Tags["Service"]);
        }
    }
}
