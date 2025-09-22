using Xunit;
using Ipam.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ipam.DataAccess.Tests.Entities
{
    /// <summary>
    /// Unit tests for IpAllocationEntity
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpAllocationEntityTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_Default_ShouldInitializeCollections()
        {
            // Act
            var entity = new IpAllocationEntity();

            // Assert
            Assert.NotNull(entity.Tags);
            Assert.Empty(entity.Tags);
            Assert.NotNull(entity.ChildrenIds);
            Assert.Empty(entity.ChildrenIds);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Id_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var expectedId = "ip-allocation-001";

            // Act
            entity.Id = expectedId;

            // Assert
            Assert.Equal(expectedId, entity.Id);
        }

        [Fact]
        public void Id_PropertyMapping_MapsToRowKey()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var testId = "test-ip-node-id";

            // Act
            entity.Id = testId;

            // Assert
            Assert.Equal(testId, entity.RowKey);
            Assert.Equal(testId, entity.Id);
        }

        [Fact]
        public void AddressSpaceId_PropertyMapping_MapsToPartitionKey()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var testAddressSpaceId = "test-address-space";

            // Act
            entity.AddressSpaceId = testAddressSpaceId;

            // Assert
            Assert.Equal(testAddressSpaceId, entity.PartitionKey);
            Assert.Equal(testAddressSpaceId, entity.AddressSpaceId);
        }

        [Fact]
        public void AddressSpaceId_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var expectedAddressSpaceId = "address-space-001";

            // Act
            entity.AddressSpaceId = expectedAddressSpaceId;

            // Assert
            Assert.Equal(expectedAddressSpaceId, entity.AddressSpaceId);
        }

        [Fact]
        public void Prefix_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var expectedPrefix = "10.0.1.0/24";

            // Act
            entity.Prefix = expectedPrefix;

            // Assert
            Assert.Equal(expectedPrefix, entity.Prefix);
        }

        [Fact]
        public void ParentId_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var expectedParentId = "parent-ip-001";

            // Act
            entity.ParentId = expectedParentId;

            // Assert
            Assert.Equal(expectedParentId, entity.ParentId);
        }

        [Fact]
        public void ParentId_SetToNull_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity
            {
                ParentId = "some-parent"
            };

            // Act
            entity.ParentId = null;

            // Assert
            Assert.Null(entity.ParentId);
        }

        [Fact]
        public void CreatedOn_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var expectedCreatedOn = DateTime.UtcNow;

            // Act
            entity.CreatedOn = expectedCreatedOn;

            // Assert
            Assert.Equal(expectedCreatedOn, entity.CreatedOn);
        }

        [Fact]
        public void ModifiedOn_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var expectedModifiedOn = DateTime.UtcNow;

            // Act
            entity.ModifiedOn = expectedModifiedOn;

            // Assert
            Assert.Equal(expectedModifiedOn, entity.ModifiedOn);
        }

        #endregion

        #region Tags Collection Tests

        [Fact]
        public void Tags_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var tags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "US-East" }
            };

            // Act
            entity.Tags = tags;

            // Assert
            Assert.Equal(2, entity.Tags.Count);
            Assert.Equal("Production", entity.Tags["Environment"]);
            Assert.Equal("US-East", entity.Tags["Region"]);
        }

        [Fact]
        public void Tags_ModifyAfterSet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var initialTags = new Dictionary<string, string>
            {
                { "Environment", "Staging" },
                { "Region", "US-East" }
            };
            entity.Tags = initialTags;

            // Act
            var currentTags = entity.Tags;
            currentTags.Remove("Environment");
            currentTags["Region"] = "US-West";
            currentTags.Add("Team", "Backend");
            entity.Tags = currentTags;

            // Assert
            Assert.Equal(2, entity.Tags.Count);
            Assert.False(entity.Tags.ContainsKey("Environment"));
            Assert.Equal("US-West", entity.Tags["Region"]);
            Assert.Equal("Backend", entity.Tags["Team"]);
        }

        [Fact]
        public void Tags_SetToNull_ShouldThrowException()
        {
            // Arrange
            var entity = new IpAllocationEntity();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => entity.Tags = null);
        }

        [Fact]
        public void Tags_SetNewDictionary_ShouldReplaceExisting()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var initialTags = new Dictionary<string, string>
            {
                { "Environment", "Staging" }
            };
            entity.Tags = initialTags;

            var newTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "US-West" }
            };

            // Act
            entity.Tags = newTags;

            // Assert
            Assert.Equal(2, entity.Tags.Count);
            Assert.Equal("Production", entity.Tags["Environment"]);
            Assert.Equal("US-West", entity.Tags["Region"]);
        }

        #endregion

        #region ChildrenIds Collection Tests

        [Fact]
        public void ChildrenIds_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var childrenIds = new[] { "child-1", "child-2" };

            // Act
            entity.ChildrenIds = childrenIds.ToList();

            // Assert
            Assert.Equal(2, entity.ChildrenIds.Count);
            Assert.Contains("child-1", entity.ChildrenIds);
            Assert.Contains("child-2", entity.ChildrenIds);
        }

        [Fact]
        public void ChildrenIds_SetArray_ShouldReplaceExisting()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            entity.ChildrenIds = new List<string> { "old-child" };

            var newChildren = new[] { "child-1", "child-2", "child-3" };

            // Act
            entity.ChildrenIds = newChildren.ToList();

            // Assert
            Assert.Equal(3, entity.ChildrenIds.Count);
            Assert.Contains("child-1", entity.ChildrenIds);
            Assert.Contains("child-2", entity.ChildrenIds);
            Assert.Contains("child-3", entity.ChildrenIds);
            Assert.DoesNotContain("old-child", entity.ChildrenIds);
        }

        [Fact]
        public void ChildrenIds_SetToNull_ShouldThrowException()
        {
            // Arrange
            var entity = new IpAllocationEntity();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => entity.ChildrenIds = null);
        }

        [Fact]
        public void ChildrenIds_EmptyArray_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();

            // Act
            entity.ChildrenIds = new List<string>();

            // Assert
            Assert.NotNull(entity.ChildrenIds);
            Assert.Empty(entity.ChildrenIds);
        }

        #endregion

        #region Validation Tests

        [Theory]
        [InlineData("10.0.1.0/24")]
        [InlineData("192.168.1.0/24")]
        [InlineData("172.16.0.0/16")]
        [InlineData("10.0.0.0/8")]
        [InlineData("172.16.0.0/12")]
        [InlineData("2001:db8::/32")]
        [InlineData("::/0")]
        public void Prefix_ValidCidrFormats_ShouldBeAccepted(string validCidr)
        {
            // Arrange
            var entity = new IpAllocationEntity();

            // Act
            entity.Prefix = validCidr;

            // Assert
            Assert.Equal(validCidr, entity.Prefix);
        }

        #endregion

        #region Complex Scenarios Tests

        [Fact]
        public void CompleteEntity_SetAllProperties_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var createdOn = DateTime.UtcNow.AddDays(-1);
            var modifiedOn = DateTime.UtcNow;

            // Act
            entity.Id = "ip-001";
            entity.AddressSpaceId = "space-001";
            entity.Prefix = "10.0.1.0/24";
            entity.ParentId = "parent-001";
            entity.ChildrenIds = new List<string> { "child-001", "child-002" };
            entity.Tags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "US-East" },
                { "Team", "Backend" }
            };
            entity.CreatedOn = createdOn;
            entity.ModifiedOn = modifiedOn;

            // Assert
            Assert.Equal("ip-001", entity.Id);
            Assert.Equal("space-001", entity.AddressSpaceId);
            Assert.Equal("10.0.1.0/24", entity.Prefix);
            Assert.Equal("parent-001", entity.ParentId);
            Assert.Equal(2, entity.ChildrenIds.Count);
            Assert.Contains("child-001", entity.ChildrenIds);
            Assert.Contains("child-002", entity.ChildrenIds);
            Assert.Equal(3, entity.Tags.Count);
            Assert.Equal("Production", entity.Tags["Environment"]);
            Assert.Equal("US-East", entity.Tags["Region"]);
            Assert.Equal("Backend", entity.Tags["Team"]);
            Assert.Equal(createdOn, entity.CreatedOn);
            Assert.Equal(modifiedOn, entity.ModifiedOn);
        }

        [Fact]
        public void Entity_CopyFromAnother_ShouldCopyAllProperties()
        {
            // Arrange
            var sourceEntity = new IpAllocationEntity
            {
                Id = "source-ip",
                AddressSpaceId = "source-space",
                Prefix = "192.168.1.0/24",
                ParentId = "source-parent",
                ChildrenIds = new List<string> { "source-child-1", "source-child-2" },
                Tags = new Dictionary<string, string>
                {
                    { "Environment", "Staging" },
                    { "Owner", "TeamA" }
                },
                CreatedOn = DateTime.UtcNow.AddDays(-2),
                ModifiedOn = DateTime.UtcNow.AddDays(-1)
            };

            var targetEntity = new IpAllocationEntity();

            // Act
            targetEntity.Id = sourceEntity.Id;
            targetEntity.AddressSpaceId = sourceEntity.AddressSpaceId;
            targetEntity.Prefix = sourceEntity.Prefix;
            targetEntity.ParentId = sourceEntity.ParentId;
            targetEntity.ChildrenIds = sourceEntity.ChildrenIds;
            targetEntity.Tags = new Dictionary<string, string>(sourceEntity.Tags);
            targetEntity.CreatedOn = sourceEntity.CreatedOn;
            targetEntity.ModifiedOn = sourceEntity.ModifiedOn;

            // Assert
            Assert.Equal(sourceEntity.Id, targetEntity.Id);
            Assert.Equal(sourceEntity.AddressSpaceId, targetEntity.AddressSpaceId);
            Assert.Equal(sourceEntity.Prefix, targetEntity.Prefix);
            Assert.Equal(sourceEntity.ParentId, targetEntity.ParentId);
            Assert.Equal(sourceEntity.ChildrenIds.Count, targetEntity.ChildrenIds.Count);
            Assert.Contains("source-child-1", targetEntity.ChildrenIds);
            Assert.Contains("source-child-2", targetEntity.ChildrenIds);
            Assert.Equal(sourceEntity.Tags.Count, targetEntity.Tags.Count);
            Assert.Equal(sourceEntity.CreatedOn, targetEntity.CreatedOn);
            Assert.Equal(sourceEntity.ModifiedOn, targetEntity.ModifiedOn);

            // Verify collections are independent
            targetEntity.Tags.Add("NewTag", "NewValue");
            Assert.DoesNotContain("NewTag", sourceEntity.Tags.Keys);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void Entity_WithLargeTags_ShouldHandleCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var largeTags = new Dictionary<string, string>();

            // Add many tags
            for (int i = 0; i < 100; i++)
            {
                largeTags.Add($"Tag{i}", $"Value{i}");
            }

            // Act
            entity.Tags = largeTags;

            // Assert
            Assert.Equal(100, entity.Tags.Count);
            Assert.Equal("Value50", entity.Tags["Tag50"]);
        }

        [Fact]
        public void Entity_WithManyChildren_ShouldHandleCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            var manyChildren = new List<string>(50);

            for (int i = 0; i < 50; i++)
            {
                manyChildren[i] = $"child-{i:D3}";
            }

            // Act
            entity.ChildrenIds = manyChildren;

            // Assert
            Assert.Equal(50, entity.ChildrenIds.Count);
            Assert.Contains("child-025", entity.ChildrenIds);
        }

        [Fact]
        public void Entity_WithSpecialCharactersInTags_ShouldHandleCorrectly()
        {
            // Arrange
            var entity = new IpAllocationEntity();

            // Act
            entity.Tags = new Dictionary<string, string>
            {
                { "Special-Tag_123", "Value with spaces & symbols!" },
                { "Unicode-Tag", "Ünïcødé Vålüé" }
            };

            // Assert
            Assert.Equal(2, entity.Tags.Count);
            Assert.Equal("Value with spaces & symbols!", entity.Tags["Special-Tag_123"]);
            Assert.Equal("Ünïcødé Vålüé", entity.Tags["Unicode-Tag"]);
        }

        [Fact]
        public void Tags_SerializationDeserialization_PreservesData()
        {
            // Arrange
            var entity = new IpAllocationEntity();
            entity.Tags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "USEast" },
                { "Application", "WebServer" }
            };

            // Act - Simulate serialization/deserialization
            var serializedTags = System.Text.Json.JsonSerializer.Serialize(entity.Tags);
            var deserializedTags = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(serializedTags);

            // Assert
            Assert.Equal(entity.Tags.Count, deserializedTags.Count);
            Assert.Equal(entity.Tags["Environment"], deserializedTags["Environment"]);
            Assert.Equal(entity.Tags["Region"], deserializedTags["Region"]);
            Assert.Equal(entity.Tags["Application"], deserializedTags["Application"]);
        }

        #endregion
    }
}