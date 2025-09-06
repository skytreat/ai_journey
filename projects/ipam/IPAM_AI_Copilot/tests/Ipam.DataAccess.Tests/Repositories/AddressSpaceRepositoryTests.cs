using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Repositories;
using Ipam.DataAccess.Models;
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
            _configMock.Setup(x => x.GetConnectionString("AzureTableStorage"))
                .Returns("UseDevelopmentStorage=true");
            
            _repository = new AddressSpaceRepository(_configMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidAddressSpace_ShouldSucceed()
        {
            // Arrange
            var addressSpace = new AddressSpace
            {
                PartitionKey = "partition1",
                RowKey = "row1",
                Name = "Test Space",
                Description = "Test Description"
            };

            // Act
            var result = await _repository.CreateAsync(addressSpace);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(addressSpace.Name, result.Name);
        }
    }
}
