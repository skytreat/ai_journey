using Xunit;
using Ipam.DataAccess.Entities;
using Azure;
using System;

namespace Ipam.DataAccess.Tests.Entities
{
    /// <summary>
    /// Comprehensive unit tests for AddressSpace model
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceEntityTests
    {
        [Fact]
        public void Constructor_DefaultValues_SetsCorrectDefaults()
        {
            // Act
            var addressSpace = new AddressSpaceEntity();

            // Assert
            Assert.Null(addressSpace.Status); // Status is not initialized by default
            Assert.Null(addressSpace.Name);
            Assert.Null(addressSpace.Description);
            Assert.Equal(DateTime.MinValue, addressSpace.CreatedOn); // DateTime default value
            Assert.Equal(DateTime.MinValue, addressSpace.ModifiedOn); // DateTime default value
        }

        [Fact]
        public void Id_PropertyMapping_MapsToRowKey()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
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
            var addressSpace = new AddressSpaceEntity();
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
            var addressSpace = new AddressSpaceEntity();

            // Act
            addressSpace.Status = status;

            // Assert
            Assert.Equal(status, addressSpace.Status);
        }

        [Fact]
        public void Name_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testName = "Test Address Space";

            // Act
            addressSpace.Name = testName;

            // Assert
            Assert.Equal(testName, addressSpace.Name);
        }

        [Fact]
        public void Description_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testDescription = "Test Description";

            // Act
            addressSpace.Description = testDescription;

            // Assert
            Assert.Equal(testDescription, addressSpace.Description);
        }

        [Fact]
        public void CreatedOn_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testDate = DateTime.UtcNow.AddDays(-1);

            // Act
            addressSpace.CreatedOn = testDate;

            // Assert
            Assert.Equal(testDate, addressSpace.CreatedOn);
        }

        [Fact]
        public void ModifiedOn_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testDate = DateTime.UtcNow;

            // Act
            addressSpace.ModifiedOn = testDate;

            // Assert
            Assert.Equal(testDate, addressSpace.ModifiedOn);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Name_InvalidValues_AllowsButShouldBeValidatedElsewhere(string invalidName)
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();

            // Act & Assert
            // The model itself doesn't validate, validation should be done at service/controller level
            addressSpace.Name = invalidName;
            Assert.Equal(invalidName, addressSpace.Name);
        }

        [Fact]
        public void Description_LongText_AcceptsLongDescriptions()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var longDescription = new string('A', 1000);

            // Act
            addressSpace.Description = longDescription;

            // Assert
            Assert.Equal(longDescription, addressSpace.Description);
            Assert.Equal(1000, addressSpace.Description.Length);
        }

        [Fact]
        public void PartitionKey_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testPartitionKey = "AddressSpaces";

            // Act
            addressSpace.PartitionKey = testPartitionKey;

            // Assert
            Assert.Equal(testPartitionKey, addressSpace.PartitionKey);
        }

        [Fact]
        public void ETag_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testETag = new ETag("test-etag");

            // Act
            addressSpace.ETag = testETag;

            // Assert
            Assert.Equal(testETag, addressSpace.ETag);
        }

        [Fact]
        public void Timestamp_SetAndGet_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testTimestamp = DateTimeOffset.UtcNow;

            // Act
            addressSpace.Timestamp = testTimestamp;

            // Assert
            Assert.Equal(testTimestamp, addressSpace.Timestamp);
        }

        [Fact]
        public void CompleteEntity_SetAllProperties_WorksCorrectly()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity();
            var testId = "space-123";
            var testName = "Test Space";
            var testDescription = "Test Description";
            var testPartitionKey = "AddressSpaces";
            var testStatus = "Active";
            var testCreatedOn = DateTime.UtcNow.AddDays(-1);
            var testModifiedOn = DateTime.UtcNow;
            var testETag = new ETag("test-etag");
            var testTimestamp = DateTimeOffset.UtcNow;

            // Act
            addressSpace.Id = testId;
            addressSpace.Name = testName;
            addressSpace.Description = testDescription;
            addressSpace.PartitionKey = testPartitionKey;
            addressSpace.Status = testStatus;
            addressSpace.CreatedOn = testCreatedOn;
            addressSpace.ModifiedOn = testModifiedOn;
            addressSpace.ETag = testETag;
            addressSpace.Timestamp = testTimestamp;

            // Assert
            Assert.Equal(testId, addressSpace.Id);
            Assert.Equal(testId, addressSpace.RowKey); // Id maps to RowKey
            Assert.Equal(testName, addressSpace.Name);
            Assert.Equal(testDescription, addressSpace.Description);
            Assert.Equal(testPartitionKey, addressSpace.PartitionKey);
            Assert.Equal(testStatus, addressSpace.Status);
            Assert.Equal(testCreatedOn, addressSpace.CreatedOn);
            Assert.Equal(testModifiedOn, addressSpace.ModifiedOn);
            Assert.Equal(testETag, addressSpace.ETag);
            Assert.Equal(testTimestamp, addressSpace.Timestamp);
        }

        [Fact]
        public void Entity_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var addressSpace = new AddressSpaceEntity();

            // Assert
            Assert.Null(addressSpace.PartitionKey);
            Assert.Null(addressSpace.RowKey);
            Assert.Null(addressSpace.Id); // Id returns RowKey, which is null by default
            Assert.Null(addressSpace.Timestamp);
            Assert.Equal(default(ETag), addressSpace.ETag);
            Assert.Null(addressSpace.Name);
            Assert.Null(addressSpace.Description);
            Assert.Equal(DateTime.MinValue, addressSpace.CreatedOn);
            Assert.Equal(DateTime.MinValue, addressSpace.ModifiedOn);
            Assert.Null(addressSpace.Status);
        }
    }
}