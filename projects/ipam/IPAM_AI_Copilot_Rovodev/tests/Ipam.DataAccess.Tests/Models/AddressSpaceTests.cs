using Xunit;
using Ipam.DataAccess.Models;
using System;

namespace Ipam.DataAccess.Tests.Models
{
    /// <summary>
    /// Comprehensive unit tests for AddressSpace model
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsCorrectDefaults()
        {
            // Act
            var addressSpace = new AddressSpace();

            // Assert
            Assert.NotNull(addressSpace.Tags);
            Assert.Empty(addressSpace.Tags);
            Assert.NotNull(addressSpace.Metadata);
            Assert.Empty(addressSpace.Metadata);
            Assert.Equal("Active", addressSpace.Status);
        }

        [Fact]
        public void Id_PropertyMapping_MapsToRowKey()
        {
            // Arrange
            var addressSpace = new AddressSpace();
            var testId = "test-address-space-id";

            // Act
            addressSpace.Id = testId;

            // Assert
            Assert.Equal(testId, addressSpace.RowKey);
            Assert.Equal(testId, addressSpace.Id);
        }

        [Fact]
        public void Id_RowKeyMapping_MapsToId()
        {
            // Arrange
            var addressSpace = new AddressSpace();
            var testId = "test-row-key";

            // Act
            addressSpace.RowKey = testId;

            // Assert
            Assert.Equal(testId, addressSpace.Id);
            Assert.Equal(testId, addressSpace.RowKey);
        }

        [Theory]
        [InlineData("Active")]
        [InlineData("Inactive")]
        [InlineData("Archived")]
        [InlineData("Maintenance")]
        public void Status_ValidValues_AcceptsValidStatuses(string status)
        {
            // Arrange
            var addressSpace = new AddressSpace();

            // Act
            addressSpace.Status = status;

            // Assert
            Assert.Equal(status, addressSpace.Status);
        }

        [Fact]
        public void Tags_Modification_AllowsTagManipulation()
        {
            // Arrange
            var addressSpace = new AddressSpace();

            // Act
            addressSpace.Tags["Environment"] = "Production";
            addressSpace.Tags["Region"] = "USEast";

            // Assert
            Assert.Equal(2, addressSpace.Tags.Count);
            Assert.Equal("Production", addressSpace.Tags["Environment"]);
            Assert.Equal("USEast", addressSpace.Tags["Region"]);
        }

        [Fact]
        public void Metadata_Modification_AllowsMetadataManipulation()
        {
            // Arrange
            var addressSpace = new AddressSpace();

            // Act
            addressSpace.Metadata["Owner"] = "NetworkTeam";
            addressSpace.Metadata["CostCenter"] = "IT-001";

            // Assert
            Assert.Equal(2, addressSpace.Metadata.Count);
            Assert.Equal("NetworkTeam", addressSpace.Metadata["Owner"]);
            Assert.Equal("IT-001", addressSpace.Metadata["CostCenter"]);
        }

        [Fact]
        public void CreatedOn_DefaultValue_IsNotMinValue()
        {
            // Arrange & Act
            var addressSpace = new AddressSpace();

            // Assert
            Assert.NotEqual(DateTime.MinValue, addressSpace.CreatedOn);
            Assert.True(addressSpace.CreatedOn <= DateTime.UtcNow);
        }

        [Fact]
        public void ModifiedOn_DefaultValue_IsNotMinValue()
        {
            // Arrange & Act
            var addressSpace = new AddressSpace();

            // Assert
            Assert.NotEqual(DateTime.MinValue, addressSpace.ModifiedOn);
            Assert.True(addressSpace.ModifiedOn <= DateTime.UtcNow);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Name_InvalidValues_AllowsButShouldBeValidatedElsewhere(string invalidName)
        {
            // Arrange
            var addressSpace = new AddressSpace();

            // Act & Assert
            // The model itself doesn't validate, validation should be done at service/controller level
            addressSpace.Name = invalidName;
            Assert.Equal(invalidName, addressSpace.Name);
        }

        [Fact]
        public void Description_LongText_AcceptsLongDescriptions()
        {
            // Arrange
            var addressSpace = new AddressSpace();
            var longDescription = new string('A', 1000);

            // Act
            addressSpace.Description = longDescription;

            // Assert
            Assert.Equal(longDescription, addressSpace.Description);
            Assert.Equal(1000, addressSpace.Description.Length);
        }

        [Fact]
        public void PartitionKey_DefaultValue_IsCorrect()
        {
            // Arrange & Act
            var addressSpace = new AddressSpace();

            // Assert
            // Default partition key should be set for proper Azure Table Storage partitioning
            Assert.NotNull(addressSpace.PartitionKey);
        }

        [Fact]
        public void ETag_DefaultValue_IsNotNull()
        {
            // Arrange & Act
            var addressSpace = new AddressSpace();

            // Assert
            Assert.NotNull(addressSpace.ETag);
        }

        [Fact]
        public void Timestamp_DefaultValue_IsNotNull()
        {
            // Arrange & Act
            var addressSpace = new AddressSpace();

            // Assert
            Assert.NotNull(addressSpace.Timestamp);
        }

        [Fact]
        public void Tags_NullAssignment_ThrowsException()
        {
            // Arrange
            var addressSpace = new AddressSpace();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => addressSpace.Tags = null);
        }

        [Fact]
        public void Metadata_NullAssignment_ThrowsException()
        {
            // Arrange
            var addressSpace = new AddressSpace();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => addressSpace.Metadata = null);
        }

        [Fact]
        public void ToString_WithNameAndId_ReturnsFormattedString()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                Id = "space-123",
                Name = "Test Space"
            };

            // Act
            var result = addressSpace.ToString();

            // Assert
            Assert.Contains("space-123", result);
            Assert.Contains("Test Space", result);
        }

        [Fact]
        public void Equals_SameId_ReturnsTrue()
        {
            // Arrange
            var addressSpace1 = new AddressSpace { Id = "space-123" };
            var addressSpace2 = new AddressSpace { Id = "space-123" };

            // Act
            var result = addressSpace1.Equals(addressSpace2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_DifferentId_ReturnsFalse()
        {
            // Arrange
            var addressSpace1 = new AddressSpace { Id = "space-123" };
            var addressSpace2 = new AddressSpace { Id = "space-456" };

            // Act
            var result = addressSpace1.Equals(addressSpace2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetHashCode_SameId_ReturnsSameHashCode()
        {
            // Arrange
            var addressSpace1 = new AddressSpace { Id = "space-123" };
            var addressSpace2 = new AddressSpace { Id = "space-123" };

            // Act
            var hash1 = addressSpace1.GetHashCode();
            var hash2 = addressSpace2.GetHashCode();

            // Assert
            Assert.Equal(hash1, hash2);
        }
    }
}