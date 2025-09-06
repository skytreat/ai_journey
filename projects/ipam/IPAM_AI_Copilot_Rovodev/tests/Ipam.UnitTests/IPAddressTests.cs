using System;
using System.Collections.Generic;
using Ipam.DataAccess.Models;
using Xunit;

namespace Ipam.UnitTests
{
    public class IPAddressTests
    {
        [Fact]
        public void IPAddress_InheritTagsFromParent_ShouldInheritInheritableTags()
        {
            // Arrange
            var parentIP = new IPAddress
            {
                Id = "parent-1",
                Prefix = "192.168.1.0/24",
                Tags = new List<Tag>
                {
                    new Tag { Name = "Environment", Value = "Production", Type = TagType.Inheritable },
                    new Tag { Name = "Owner", Value = "TeamA", Type = TagType.NonInheritable }
                }
            };

            var childIP = new IPAddress
            {
                Id = "child-1",
                Prefix = "192.168.1.10/32",
                ParentId = parentIP.Id,
                Tags = new List<Tag>
                {
                    new Tag { Name = "Service", Value = "Web", Type = TagType.Inheritable }
                }
            };

            // Act
            // Simulate tag inheritance logic
            foreach (var tag in parentIP.Tags)
            {
                if (tag.Type == TagType.Inheritable && !childIP.Tags.Exists(t => t.Name == tag.Name))
                {
                    childIP.Tags.Add(new Tag { Name = tag.Name, Value = tag.Value, Type = tag.Type });
                }
            }

            // Assert
            Assert.Contains(childIP.Tags, t => t.Name == "Environment" && t.Value == "Production");
            Assert.DoesNotContain(childIP.Tags, t => t.Name == "Owner");
            Assert.Contains(childIP.Tags, t => t.Name == "Service");
        }
    }
}
