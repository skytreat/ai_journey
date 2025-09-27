using Xunit;
using Ipam.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ipam.DataAccess.Tests.Entities
{
    /// <summary>
    /// Unit tests for TagEntity
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagEntityTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_Default_ShouldInitializeCollections()
        {
            // Act
            var entity = new TagEntity();

            // Assert
            Assert.NotNull(entity.KnownValues);
            Assert.Empty(entity.KnownValues);
            Assert.NotNull(entity.Implies);
            Assert.Empty(entity.Implies);
            Assert.NotNull(entity.Attributes);
            Assert.Empty(entity.Attributes);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Name_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
            var expectedName = "Environment";

            // Act
            entity.Name = expectedName;

            // Assert
            Assert.Equal(expectedName, entity.Name);
        }

        [Fact]
        public void AddressSpaceId_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
            var expectedAddressSpaceId = "address-space-001";

            // Act
            entity.AddressSpaceId = expectedAddressSpaceId;

            // Assert
            Assert.Equal(expectedAddressSpaceId, entity.AddressSpaceId);
        }

        [Fact]
        public void Description_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
            var expectedDescription = "Environment classification tag";

            // Act
            entity.Description = expectedDescription;

            // Assert
            Assert.Equal(expectedDescription, entity.Description);
        }

        [Fact]
        public void Description_SetToNull_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity
            {
                Description = "Some description"
            };

            // Act
            entity.Description = null;

            // Assert
            Assert.Null(entity.Description);
        }

        [Fact]
        public void Type_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
            var expectedType = "Inheritable";

            // Act
            entity.Type = expectedType;

            // Assert
            Assert.Equal(expectedType, entity.Type);
        }

        [Fact]
        public void CreatedOn_SetAndGet_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
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
            var entity = new TagEntity();
            var expectedModifiedOn = DateTime.UtcNow;

            // Act
            entity.ModifiedOn = expectedModifiedOn;

            // Assert
            Assert.Equal(expectedModifiedOn, entity.ModifiedOn);
        }

        #endregion

        #region KnownValues Collection Tests

        [Fact]
        public void KnownValues_AddValue_ShouldAddSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();
            var knownValues = new List<string>();

            // Act
            knownValues.Add("Production");
            knownValues.Add("Staging");
            knownValues.Add("Development");
            entity.KnownValues = knownValues;

            // Assert
            Assert.Equal(3, entity.KnownValues.Count);
            Assert.Contains("Production", entity.KnownValues);
            Assert.Contains("Staging", entity.KnownValues);
            Assert.Contains("Development", entity.KnownValues);
        }

        [Fact]
        public void KnownValues_RemoveValue_ShouldRemoveSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();
            var knownValues = new List<string> { "Production", "Staging" };
            entity.KnownValues = knownValues;

            // Act
            var currentValues = entity.KnownValues;
            currentValues.Remove("Staging");
            entity.KnownValues = currentValues;

            // Assert
            Assert.Single(entity.KnownValues);
            Assert.Contains("Production", entity.KnownValues);
            Assert.DoesNotContain("Staging", entity.KnownValues);
        }

        [Fact]
        public void KnownValues_SetNewList_ShouldReplaceExisting()
        {
            // Arrange
            var entity = new TagEntity();
            entity.KnownValues.Add("OldValue");

            var newValues = new List<string> { "Value1", "Value2", "Value3" };

            // Act
            entity.KnownValues = newValues;

            // Assert
            Assert.Equal(3, entity.KnownValues.Count);
            Assert.Contains("Value1", entity.KnownValues);
            Assert.Contains("Value2", entity.KnownValues);
            Assert.Contains("Value3", entity.KnownValues);
            Assert.DoesNotContain("OldValue", entity.KnownValues);
        }

        [Fact]
        public void KnownValues_DuplicateValues_ShouldHandleAccordingToListBehavior()
        {
            // Arrange
            var entity = new TagEntity();
            var knownValues = new List<string> { "Production", "Production" }; // Duplicate

            // Act
            entity.KnownValues = knownValues;

            // Assert
            Assert.Equal(2, entity.KnownValues.Count); // List allows duplicates
            Assert.Equal("Production", entity.KnownValues[0]);
            Assert.Equal("Production", entity.KnownValues[1]);
        }

        #endregion

        #region Implies Dictionary Tests

        [Fact]
        public void Implies_AddImplication_ShouldAddSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();
            var implies = new Dictionary<string, Dictionary<string, string>>();

            // Act
            implies.Add("Backup", new Dictionary<string, string> { { "Value", "Required" } });
            implies.Add("Monitoring", new Dictionary<string, string> { { "Value", "Enabled" } });
            entity.Implies = implies;

            // Assert
            Assert.Equal(2, entity.Implies.Count);
            Assert.Equal("Required", entity.Implies["Backup"]["Value"]);
            Assert.Equal("Enabled", entity.Implies["Monitoring"]["Value"]);
        }

        [Fact]
        public void Implies_RemoveImplication_ShouldRemoveSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();
            entity.Implies.Add("Backup", new Dictionary<string, string> { { "Value", "Required" } });
            entity.Implies.Add("Monitoring", new Dictionary<string, string> { { "Value", "Enabled" } });

            // Act
            entity.Implies.Remove("Backup");

            // Assert
            Assert.Single(entity.Implies);
            Assert.False(entity.Implies.ContainsKey("Backup"));
            Assert.True(entity.Implies.ContainsKey("Monitoring"));
        }

        [Fact]
        public void Implies_UpdateImplication_ShouldUpdateSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();
            entity.Implies.Add("Backup", new Dictionary<string, string> { { "Value", "Optional" } });

            // Act
            entity.Implies["Backup"] = new Dictionary<string, string> { { "Value", "Required" } };

            // Assert
            Assert.Single(entity.Implies);
            Assert.Equal("Required", entity.Implies["Backup"]["Value"]);
        }

        [Fact]
        public void Implies_SetNewDictionary_ShouldReplaceExisting()
        {
            // Arrange
            var entity = new TagEntity();
            entity.Implies.Add("OldKey", new Dictionary<string, string> { { "Value", "OldValue" } });

            var newImplies = new Dictionary<string, Dictionary<string, string>>
            {
                { "Backup", new Dictionary<string, string> { { "Value", "Required" } } },
                { "Monitoring", new Dictionary<string, string> { { "Value", "Enabled" } } },
                { "Compliance", new Dictionary<string, string> { { "Value", "SOX" } } }
            };

            // Act
            entity.Implies = newImplies;

            // Assert
            Assert.Equal(3, entity.Implies.Count);
            Assert.Equal("Required", entity.Implies["Backup"]["Value"]);
            Assert.Equal("Enabled", entity.Implies["Monitoring"]["Value"]);
            Assert.Equal("SOX", entity.Implies["Compliance"]["Value"]);
            Assert.False(entity.Implies.ContainsKey("OldKey"));
        }

        #endregion

        #region Attributes Dictionary Tests

        [Fact]
        public void Attributes_AddAttribute_ShouldAddSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.Attributes.Add("Color", new Dictionary<string, string> { { "Value", "Blue" } });
            entity.Attributes.Add("Icon", new Dictionary<string, string> { { "Value", "server" } });
            entity.Attributes.Add("Priority", new Dictionary<string, string> { { "Value", "High" } });

            // Assert
            Assert.Equal(3, entity.Attributes.Count);
            Assert.Equal("Blue", entity.Attributes["Color"]["Value"]);
            Assert.Equal("server", entity.Attributes["Icon"]["Value"]);
            Assert.Equal("High", entity.Attributes["Priority"]["Value"]);
        }

        [Fact]
        public void Attributes_RemoveAttribute_ShouldRemoveSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();
            entity.Attributes.Add("Color", new Dictionary<string, string> { { "Value", "Blue" } });
            entity.Attributes.Add("Icon", new Dictionary<string, string> { { "Value", "server" } });

            // Act
            entity.Attributes.Remove("Color");

            // Assert
            Assert.Single(entity.Attributes);
            Assert.False(entity.Attributes.ContainsKey("Color"));
            Assert.True(entity.Attributes.ContainsKey("Icon"));
        }

        [Fact]
        public void Attributes_UpdateAttribute_ShouldUpdateSuccessfully()
        {
            // Arrange
            var entity = new TagEntity();
            entity.Attributes.Add("Color", new Dictionary<string, string> { { "Value", "Red" } });

            // Act
            entity.Attributes["Color"] = new Dictionary<string, string> { { "Value", "Blue" } };

            // Assert
            Assert.Single(entity.Attributes);
            Assert.Equal("Blue", entity.Attributes["Color"]["Value"]);
        }

        [Fact]
        public void Attributes_SetNewDictionary_ShouldReplaceExisting()
        {
            // Arrange
            var entity = new TagEntity();
            entity.Attributes.Add("OldAttribute", new Dictionary<string, string> { { "Value", "OldValue" } });

            var newAttributes = new Dictionary<string, Dictionary<string, string>>
            {
                { "Color", new Dictionary<string, string> { { "Value", "Green" } } },
                { "Icon", new Dictionary<string, string> { { "Value", "database" } } },
                { "Size", new Dictionary<string, string> { { "Value", "Large" } } }
            };

            // Act
            entity.Attributes = newAttributes;

            // Assert
            Assert.Equal(3, entity.Attributes.Count);
            Assert.Equal("Green", entity.Attributes["Color"]["Value"]);
            Assert.Equal("database", entity.Attributes["Icon"]["Value"]);
            Assert.Equal("Large", entity.Attributes["Size"]["Value"]);
            Assert.False(entity.Attributes.ContainsKey("OldAttribute"));
        }

        #endregion

        #region Tag Type Validation Tests

        [Theory]
        [InlineData("Inheritable")]
        [InlineData("Enumerated")]
        [InlineData("FreeForm")]
        [InlineData("System")]
        public void Type_ValidTagTypes_ShouldBeAccepted(string validType)
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.Type = validType;

            // Assert
            Assert.Equal(validType, entity.Type);
        }

        [Fact]
        public void Type_EmptyString_ShouldBeAccepted()
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.Type = "";

            // Assert
            Assert.Equal("", entity.Type);
        }

        [Fact]
        public void Type_NullValue_ShouldBeAccepted()
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.Type = null;

            // Assert
            Assert.Null(entity.Type);
        }

        #endregion

        #region Complex Scenarios Tests

        [Fact]
        public void CompleteEntity_SetAllProperties_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
            var createdOn = DateTime.UtcNow.AddDays(-1);
            var modifiedOn = DateTime.UtcNow;

            // Act
            entity.Name = "Environment";
            entity.AddressSpaceId = "space-001";
            entity.Description = "Environment classification tag";
            entity.Type = "Inheritable";
            entity.CreatedOn = createdOn;
            entity.ModifiedOn = modifiedOn;
            entity.KnownValues = new List<string> { "Production", "Staging", "Development" };
            entity.Implies = new Dictionary<string, Dictionary<string, string>>
            {
                { "Backup", new Dictionary<string, string> { { "Value", "Required" } } },
                { "Monitoring", new Dictionary<string, string> { { "Value", "Enabled" } } }
            };
            entity.Attributes = new Dictionary<string, Dictionary<string, string>>
            {
                { "Color", new Dictionary<string, string> { { "Value", "Blue" } } },
                { "Icon", new Dictionary<string, string> { { "Value", "server" } } },
                { "Priority", new Dictionary<string, string> { { "Value", "High" } } }
            };

            // Assert
            Assert.Equal("Environment", entity.Name);
            Assert.Equal("space-001", entity.AddressSpaceId);
            Assert.Equal("Environment classification tag", entity.Description);
            Assert.Equal("Inheritable", entity.Type);
            Assert.Equal(createdOn, entity.CreatedOn);
            Assert.Equal(modifiedOn, entity.ModifiedOn);
            Assert.Equal(3, entity.KnownValues.Count);
            Assert.Contains("Production", entity.KnownValues);
            Assert.Contains("Staging", entity.KnownValues);
            Assert.Contains("Development", entity.KnownValues);
            Assert.Equal(2, entity.Implies.Count);
            Assert.Equal("Required", entity.Implies["Backup"]["Value"]);
            Assert.Equal("Enabled", entity.Implies["Monitoring"]["Value"]);
            Assert.Equal(3, entity.Attributes.Count);
            Assert.Equal("Blue", entity.Attributes["Color"]["Value"]);
            Assert.Equal("server", entity.Attributes["Icon"]["Value"]);
            Assert.Equal("High", entity.Attributes["Priority"]["Value"]);
        }

        [Fact]
        public void Entity_CopyFromAnother_ShouldCopyAllProperties()
        {
            // Arrange
            var sourceEntity = new TagEntity
            {
                Name = "Region",
                AddressSpaceId = "source-space",
                Description = "Regional classification",
                Type = "Enumerated",
                CreatedOn = DateTime.UtcNow.AddDays(-2),
                ModifiedOn = DateTime.UtcNow.AddDays(-1),
                KnownValues = new List<string> { "US-East", "US-West", "EU-Central" },
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Timezone", new Dictionary<string, string> { { "Value", "Auto" } } },
                    { "Compliance", new Dictionary<string, string> { { "Value", "GDPR" } } }
                },
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Color", new Dictionary<string, string> { { "Value", "Red" } } },
                    { "MapIcon", new Dictionary<string, string> { { "Value", "location" } } }
                }
            };

            var targetEntity = new TagEntity();

            // Act
            targetEntity.Name = sourceEntity.Name;
            targetEntity.AddressSpaceId = sourceEntity.AddressSpaceId;
            targetEntity.Description = sourceEntity.Description;
            targetEntity.Type = sourceEntity.Type;
            targetEntity.CreatedOn = sourceEntity.CreatedOn;
            targetEntity.ModifiedOn = sourceEntity.ModifiedOn;
            targetEntity.KnownValues = new List<string>(sourceEntity.KnownValues);
            // Deep copy required for nested dictionaries to ensure test independence
            targetEntity.Implies = sourceEntity.Implies.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<string, string>(kvp.Value));
            targetEntity.Attributes = sourceEntity.Attributes.ToDictionary(kvp => kvp.Key, kvp => new Dictionary<string, string>(kvp.Value));

            // Assert
            Assert.Equal(sourceEntity.Name, targetEntity.Name);
            Assert.Equal(sourceEntity.AddressSpaceId, targetEntity.AddressSpaceId);
            Assert.Equal(sourceEntity.Description, targetEntity.Description);
            Assert.Equal(sourceEntity.Type, targetEntity.Type);
            Assert.Equal(sourceEntity.CreatedOn, targetEntity.CreatedOn);
            Assert.Equal(sourceEntity.ModifiedOn, targetEntity.ModifiedOn);
            Assert.Equal(sourceEntity.KnownValues.Count, targetEntity.KnownValues.Count);
            Assert.Equal(sourceEntity.Implies.Count, targetEntity.Implies.Count);
            Assert.Equal(sourceEntity.Attributes.Count, targetEntity.Attributes.Count);

            // Verify collections are independent
            targetEntity.KnownValues.Add("Asia-Pacific");
            targetEntity.Implies.Add("Currency", new Dictionary<string, string> { { "Value", "Local" } });
            targetEntity.Attributes.Add("Flag", new Dictionary<string, string> { { "Value", "country-flag" } });

            Assert.DoesNotContain("Asia-Pacific", sourceEntity.KnownValues);
            Assert.DoesNotContain("Currency", sourceEntity.Implies.Keys);
            Assert.DoesNotContain("Flag", sourceEntity.Attributes.Keys);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void Entity_WithLargeKnownValues_ShouldHandleCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
            var largeKnownValues = new List<string>();

            // Add many known values
            for (int i = 0; i < 1000; i++)
            {
                largeKnownValues.Add($"Value{i:D4}");
            }

            // Act
            entity.KnownValues = largeKnownValues;

            // Assert
            Assert.Equal(1000, entity.KnownValues.Count);
            Assert.Contains("Value0500", entity.KnownValues);
        }

        [Fact]
        public void Entity_WithManyImplications_ShouldHandleCorrectly()
        {
            // Arrange
            var entity = new TagEntity();
            var manyImplications = new Dictionary<string, Dictionary<string, string>>();

            for (int i = 0; i < 100; i++)
            {
                manyImplications.Add($"ImpliedTag{i}", new Dictionary<string, string> { { "Value", $"ImpliedValue{i}" } });
            }

            // Act
            entity.Implies = manyImplications;

            // Assert
            Assert.Equal(100, entity.Implies.Count);
            Assert.Equal("ImpliedValue50", entity.Implies["ImpliedTag50"]["Value"]);
        }

        [Fact]
        public void Entity_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.Name = "Special-Tag_123";
            entity.Description = "Description with special chars: !@#$%^&*()";
            var knownValues = new List<string> { "Value with spaces & symbols!", "Ünïcødé Vålüé" };
            entity.KnownValues = knownValues;
            var implies = new Dictionary<string, Dictionary<string, string>>
            {
                { "Special-Key", new Dictionary<string, string> { { "Value", "Special-Value with symbols!" } } }
            };
            entity.Implies = implies;
            var attributes = new Dictionary<string, Dictionary<string, string>>
            {
                { "Unicode-Attr", new Dictionary<string, string> { { "Value", "Ünïcødé Åttrïbütë" } } }
            };
            entity.Attributes = attributes;

            // Assert
            Assert.Equal("Special-Tag_123", entity.Name);
            Assert.Contains("Description with special chars: !@#$%^&*()", entity.Description);
            Assert.Contains("Value with spaces & symbols!", entity.KnownValues);
            Assert.Contains("Ünïcødé Vålüé", entity.KnownValues);
            Assert.Equal("Special-Value with symbols!", entity.Implies["Special-Key"]["Value"]);
            Assert.Equal("Ünïcødé Åttrïbütë", entity.Attributes["Unicode-Attr"]["Value"]);
        }

        [Fact]
        public void Entity_WithEmptyCollections_ShouldWorkCorrectly()
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.KnownValues = new List<string>();
            entity.Implies = new Dictionary<string, Dictionary<string, string>>();
            entity.Attributes = new Dictionary<string, Dictionary<string, string>>();

            // Assert
            Assert.NotNull(entity.KnownValues);
            Assert.Empty(entity.KnownValues);
            Assert.NotNull(entity.Implies);
            Assert.Empty(entity.Implies);
            Assert.NotNull(entity.Attributes);
            Assert.Empty(entity.Attributes);
        }

        #endregion

        #region Business Logic Tests

        [Fact]
        public void InheritableTag_WithImplications_ShouldBeValid()
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.Type = "Inheritable";
            entity.Implies.Add("Backup", new Dictionary<string, string> { { "Value", "Required" } });
            entity.Implies.Add("Monitoring", new Dictionary<string, string> { { "Value", "Enabled" } });

            // Assert
            Assert.Equal("Inheritable", entity.Type);
            Assert.Equal(2, entity.Implies.Count);
            Assert.True(entity.Implies.ContainsKey("Backup"));
            Assert.True(entity.Implies.ContainsKey("Monitoring"));
        }

        [Fact]
        public void EnumeratedTag_WithKnownValues_ShouldBeValid()
        {
            // Arrange
            var entity = new TagEntity();
            var knownValues = new List<string>();

            // Act
            entity.Type = "Enumerated";
            knownValues.Add("Option1");
            knownValues.Add("Option2");
            knownValues.Add("Option3");
            entity.KnownValues = knownValues;

            // Assert
            Assert.Equal("Enumerated", entity.Type);
            Assert.Equal(3, entity.KnownValues.Count);
            Assert.Contains("Option1", entity.KnownValues);
            Assert.Contains("Option2", entity.KnownValues);
            Assert.Contains("Option3", entity.KnownValues);
        }

        [Fact]
        public void SystemTag_WithAttributes_ShouldBeValid()
        {
            // Arrange
            var entity = new TagEntity();

            // Act
            entity.Type = "System";
            entity.Attributes.Add("ReadOnly", new Dictionary<string, string> { { "Value", "true" } });
            entity.Attributes.Add("SystemGenerated", new Dictionary<string, string> { { "Value", "true" } });
            entity.Attributes.Add("Version", new Dictionary<string, string> { { "Value", "1.0" } });

            // Assert
            Assert.Equal("System", entity.Type);
            Assert.Equal(3, entity.Attributes.Count);
            Assert.Equal("true", entity.Attributes["ReadOnly"]["Value"]);
            Assert.Equal("true", entity.Attributes["SystemGenerated"]["Value"]);
            Assert.Equal("1.0", entity.Attributes["Version"]["Value"]);
        }

        #endregion
    }
}
