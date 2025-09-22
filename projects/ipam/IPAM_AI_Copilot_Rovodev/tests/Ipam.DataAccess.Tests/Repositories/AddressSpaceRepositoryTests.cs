using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Repositories;
using Ipam.DataAccess.Entities;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.Repositories
{
    /// <summary>
    /// Unit tests for AddressSpaceRepository
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceRepositoryTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly AddressSpaceRepository _repository;

        public AddressSpaceRepositoryTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["ConnectionStrings:AzureTableStorage"])
                       .Returns("UseDevelopmentStorage=true");
            
            _repository = new AddressSpaceRepository(_configMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidAddressSpace_ShouldSucceed()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity
            {
                PartitionKey = "AddressSpaces",
                RowKey = "test-space-1",
                Id = "test-space-1",
                Name = "Test Space",
                Description = "Test Description",
                Status = "Active",
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            // Act
            var result = await _repository.CreateAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpace.Name, result.Name);
            Assert.Equal(addressSpace.Description, result.Description);
            Assert.Equal(addressSpace.Status, result.Status);
        }

        [Fact]
        public async Task CreateAsync_NullAddressSpace_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.CreateAsync(null));
        }

        [Fact]
        public async Task CreateAsync_EmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity
            {
                PartitionKey = "AddressSpaces",
                RowKey = "test-space-1",
                Name = "", // Empty name
                Description = "Test Description"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.CreateAsync(addressSpace));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingAddressSpace_ShouldReturnAddressSpace()
        {
            // Arrange
            var partitionKey = "AddressSpaces";
            var addressSpaceId = "test-space-1";
            
            var addressSpace = new AddressSpaceEntity
            {
                PartitionKey = partitionKey,
                RowKey = addressSpaceId,
                Id = addressSpaceId,
                Name = "Test Space",
                Description = "Test Description",
                Status = "Active"
            };

            await _repository.CreateAsync(addressSpace);

            // Act
            var result = await _repository.GetByIdAsync(partitionKey, addressSpaceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpaceId, result.Id);
            Assert.Equal("Test Space", result.Name);
            Assert.Equal("Active", result.Status);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingAddressSpace_ShouldReturnNull()
        {
            // Arrange
            var partitionKey = "AddressSpaces";
            var nonExistingId = "non-existing";

            // Act
            var result = await _repository.GetByIdAsync(partitionKey, nonExistingId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ExistingAddressSpace_ShouldUpdateSuccessfully()
        {
            // Arrange
            var addressSpace = new AddressSpaceEntity
            {
                PartitionKey = "AddressSpaces",
                RowKey = "test-space-1",
                Id = "test-space-1",
                Name = "Original Name",
                Description = "Original Description",
                Status = "Active",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };

            await _repository.CreateAsync(addressSpace);

            // Modify the address space
            addressSpace.Name = "Updated Name";
            addressSpace.Description = "Updated Description";
            addressSpace.ModifiedOn = DateTime.UtcNow;

            // Act
            var result = await _repository.UpdateAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("Updated Description", result.Description);
            Assert.True(result.ModifiedOn > result.CreatedOn);
        }

        [Fact]
        public async Task DeleteAsync_ExistingAddressSpace_ShouldDeleteSuccessfully()
        {
            // Arrange
            var partitionKey = "AddressSpaces";
            var addressSpaceId = "test-space-1";
            
            var addressSpace = new AddressSpaceEntity
            {
                PartitionKey = partitionKey,
                RowKey = addressSpaceId,
                Id = addressSpaceId,
                Name = "Test Space"
            };

            await _repository.CreateAsync(addressSpace);

            // Act
            await _repository.DeleteAsync(partitionKey, addressSpaceId);

            // Verify deletion
            var deletedAddressSpace = await _repository.GetByIdAsync(partitionKey, addressSpaceId);
            Assert.Null(deletedAddressSpace);
        }

        [Fact]
        public async Task GetAllAsync_HasAddressSpaces_ShouldReturnAllInPartition()
        {
            // Arrange
            var partitionKey = "AddressSpaces";
            
            var addressSpaces = new[]
            {
                new AddressSpaceEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = "space-1",
                    Id = "space-1",
                    Name = "Space 1"
                },
                new AddressSpaceEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = "space-2", 
                    Id = "space-2",
                    Name = "Space 2"
                }
            };

            foreach (var space in addressSpaces)
            {
                await _repository.CreateAsync(space);
            }

            // Act
            var result = await _repository.GetAllAsync(partitionKey);

            // Assert
            Assert.NotNull(result);
            var spaces = result.ToList();
            Assert.Equal(2, spaces.Count);
            Assert.Contains(spaces, s => s.Name == "Space 1");
            Assert.Contains(spaces, s => s.Name == "Space 2");
        }

        [Fact]
        public async Task QueryAsync_WithNameFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var addressSpaces = new[]
            {
                new AddressSpaceEntity
                {
                    PartitionKey = "AddressSpaces",
                    RowKey = "prod-space",
                    Id = "prod-space",
                    Name = "Production Space"
                },
                new AddressSpaceEntity
                {
                    PartitionKey = "AddressSpaces",
                    RowKey = "test-space",
                    Id = "test-space", 
                    Name = "Test Space"
                }
            };

            foreach (var space in addressSpaces)
            {
                await _repository.CreateAsync(space);
            }

            // Act
            var result = await _repository.QueryAsync(nameFilter: "Production");

            // Assert
            Assert.NotNull(result);
            var spaces = result.ToList();
            Assert.Single(spaces);
            Assert.Equal("Production Space", spaces[0].Name);
        }

        [Fact]
        public async Task QueryAsync_WithDateFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var cutoffDate = DateTime.UtcNow.AddDays(-1);
            
            var oldSpace = new AddressSpaceEntity
            {
                PartitionKey = "AddressSpaces",
                RowKey = "old-space",
                Id = "old-space",
                Name = "Old Space",
                CreatedOn = DateTime.UtcNow.AddDays(-2)
            };

            var newSpace = new AddressSpaceEntity
            {
                PartitionKey = "AddressSpaces", 
                RowKey = "new-space",
                Id = "new-space",
                Name = "New Space",
                CreatedOn = DateTime.UtcNow
            };

            await _repository.CreateAsync(oldSpace);
            await _repository.CreateAsync(newSpace);

            // Act
            var result = await _repository.QueryAsync(createdAfter: cutoffDate);

            // Assert
            Assert.NotNull(result);
            var spaces = result.ToList();
            Assert.Single(spaces);
            Assert.Equal("New Space", spaces[0].Name);
        }
    }
}
