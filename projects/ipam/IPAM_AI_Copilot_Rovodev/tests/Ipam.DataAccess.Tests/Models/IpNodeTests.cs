using Xunit;
using Ipam.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace Ipam.DataAccess.Tests.Models
{
    /// <summary>
    /// Comprehensive unit tests for IpNode model
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpNodeTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsCorrectDefaults()
        {
            // Act
            var ipNode = new IpNode();

            // Assert
            Assert.NotNull(ipNode.Tags);
            Assert.Empty(ipNode.Tags);
            Assert.NotNull(ipNode.ChildrenIds);
            Assert.Empty(ipNode.ChildrenIds);
        }

        [Fact]
        public void Id_PropertyMapping_MapsToRowKey()
        {
            // Arrange
            var ipNode = new IpNode();
            var testId = "test-ip-node-id";

            // Act
            ipNode.Id = testId;

            // Assert
            Assert.Equal(testId, ipNode.RowKey);
            Assert.Equal(testId, ipNode.Id);
        }

        [Fact]
        public void AddressSpaceId_PropertyMapping_MapsToPartitionKey()
        {
            // Arrange
            var ipNode = new IpNode();
            var testAddressSpaceId = "test-address-space";

            // Act
            ipNode.AddressSpaceId = testAddressSpaceId;

            // Assert
            Assert.Equal(testAddressSpaceId, ipNode.PartitionKey);
            Assert.Equal(testAddressSpaceId, ipNode.AddressSpaceId);
        }

        [Theory]
        [InlineData("192.168.1.0/24")]
        [InlineData("10.0.0.0/8")]
        [InlineData("172.16.0.0/12")]
        [InlineData("2001:db8::/32")]
        [InlineData("::/0")]
        public void Prefix_ValidCidr_AcceptsValidPrefixes(string validPrefix)
        {
            // Arrange
            var ipNode = new IpNode();

            // Act
            ipNode.Prefix = validPrefix;

            // Assert
            Assert.Equal(validPrefix, ipNode.Prefix);
        }

        [Fact]
        public void Tags_Modification_AllowsTagManipulation()
        {
            // Arrange
            var ipNode = new IpNode();

            // Act
            ipNode.Tags["Environment"] = "Production";
            ipNode.Tags["Application"] = "WebServer";
            ipNode.Tags["Region"] = "USEast";

            // Assert
            Assert.Equal(3, ipNode.Tags.Count);
            Assert.Equal("Production", ipNode.Tags["Environment"]);
            Assert.Equal("WebServer", ipNode.Tags["Application"]);
            Assert.Equal("USEast", ipNode.Tags["Region"]);
        }

        [Fact]
        public void ChildrenIds_Modification_AllowsChildrenManagement()
        {
            // Arrange
            var ipNode = new IpNode();
            var childIds = new[] { "child1", "child2", "child3" };

            // Act
            ipNode.ChildrenIds = childIds;

            // Assert
            Assert.Equal(3, ipNode.ChildrenIds.Length);
            Assert.Contains("child1", ipNode.ChildrenIds);
            Assert.Contains("child2", ipNode.ChildrenIds);
            Assert.Contains("child3", ipNode.ChildrenIds);
        }

        [Fact]
        public void ParentId_Assignment_AcceptsParentReference()
        {
            // Arrange
            var ipNode = new IpNode();
            var parentId = "parent-node-id";

            // Act
            ipNode.ParentId = parentId;

            // Assert
            Assert.Equal(parentId, ipNode.ParentId);
        }

        [Fact]
        public void CreatedOn_DefaultValue_IsReasonable()
        {
            // Arrange & Act
            var ipNode = new IpNode();

            // Assert
            Assert.NotEqual(DateTime.MinValue, ipNode.CreatedOn);
            Assert.True(ipNode.CreatedOn <= DateTime.UtcNow);
            Assert.True(ipNode.CreatedOn >= DateTime.UtcNow.AddMinutes(-1)); // Should be recent
        }

        [Fact]
        public void ModifiedOn_DefaultValue_IsReasonable()
        {
            // Arrange & Act
            var ipNode = new IpNode();

            // Assert
            Assert.NotEqual(DateTime.MinValue, ipNode.ModifiedOn);
            Assert.True(ipNode.ModifiedOn <= DateTime.UtcNow);
            Assert.True(ipNode.ModifiedOn >= DateTime.UtcNow.AddMinutes(-1)); // Should be recent
        }

        [Fact]
        public void Tags_SerializationDeserialization_PreservesData()
        {
            // Arrange
            var ipNode = new IpNode();
            ipNode.Tags["Environment"] = "Production";
            ipNode.Tags["Region"] = "USEast";

            // Act - Simulate serialization/deserialization
            var serializedTags = System.Text.Json.JsonSerializer.Serialize(ipNode.Tags);
            var deserializedTags = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(serializedTags);

            // Assert
            Assert.Equal(ipNode.Tags.Count, deserializedTags.Count);
            Assert.Equal(ipNode.Tags["Environment"], deserializedTags["Environment"]);
            Assert.Equal(ipNode.Tags["Region"], deserializedTags["Region"]);
        }

        [Fact]
        public void ChildrenIds_EmptyArray_HandledCorrectly()
        {
            // Arrange
            var ipNode = new IpNode();

            // Act
            ipNode.ChildrenIds = new string[0];

            // Assert
            Assert.NotNull(ipNode.ChildrenIds);
            Assert.Empty(ipNode.ChildrenIds);
        }

        [Fact]
        public void ChildrenIds_NullAssignment_ThrowsException()
        {
            // Arrange
            var ipNode = new IpNode();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ipNode.ChildrenIds = null);
        }

        [Fact]
        public void Tags_NullAssignment_ThrowsException()
        {
            // Arrange
            var ipNode = new IpNode();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ipNode.Tags = null);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ParentId_EmptyOrNullValues_AcceptsForRootNodes(string parentId)
        {
            // Arrange
            var ipNode = new IpNode();

            // Act
            ipNode.ParentId = parentId;

            // Assert
            Assert.Equal(parentId, ipNode.ParentId);
        }

        [Fact]
        public void ToString_WithPrefixAndId_ReturnsFormattedString()
        {
            // Arrange
            var ipNode = new IpNode
            {
                Id = "node-123",
                Prefix = "192.168.1.0/24"
            };

            // Act
            var result = ipNode.ToString();

            // Assert
            Assert.Contains("node-123", result);
            Assert.Contains("192.168.1.0/24", result);
        }

        [Fact]
        public void Equals_SameId_ReturnsTrue()
        {
            // Arrange
            var ipNode1 = new IpNode { Id = "node-123" };
            var ipNode2 = new IpNode { Id = "node-123" };

            // Act
            var result = ipNode1.Equals(ipNode2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_DifferentId_ReturnsFalse()
        {
            // Arrange
            var ipNode1 = new IpNode { Id = "node-123" };
            var ipNode2 = new IpNode { Id = "node-456" };

            // Act
            var result = ipNode1.Equals(ipNode2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetHashCode_SameId_ReturnsSameHashCode()
        {
            // Arrange
            var ipNode1 = new IpNode { Id = "node-123" };
            var ipNode2 = new IpNode { Id = "node-123" };

            // Act
            var hash1 = ipNode1.GetHashCode();
            var hash2 = ipNode2.GetHashCode();

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HierarchyManagement_ParentChildRelationship_WorksCorrectly()
        {
            // Arrange
            var parentNode = new IpNode
            {
                Id = "parent-node",
                Prefix = "10.0.0.0/16",
                ChildrenIds = new[] { "child1", "child2" }
            };

            var childNode = new IpNode
            {
                Id = "child1",
                Prefix = "10.0.1.0/24",
                ParentId = "parent-node"
            };

            // Act & Assert
            Assert.Contains(childNode.Id, parentNode.ChildrenIds);
            Assert.Equal(parentNode.Id, childNode.ParentId);
        }
    }
}