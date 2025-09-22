using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ipam.DataAccess.Tests.Repositories
{
    public class TagRepositoryTests
    {
        private readonly TagRepository _repository;

        public TagRepositoryTests()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["ConnectionStrings:AzureTableStorage"])
                       .Returns("UseDevelopmentStorage=true");
            
            _repository = new TagRepository(configMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidTag_ShouldSucceed()
        {
            // Arrange
            var tag = new TagEntity
            {
                PartitionKey = "space1",
                RowKey = "Region",
                Type = "Inheritable",
                KnownValues = new List<string> { "USEast", "USWest" },
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Datacenter", new Dictionary<string, string> { { "USEast", "DC1" } } }
                }
            };

            // Act
            var result = await _repository.CreateAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tag.RowKey, result.RowKey);
        }

        [Fact]
        public async Task CreateAsync_CyclicImplication_ShouldThrowValidationException()
        {
            // Arrange
            var tag = new TagEntity
            {
                PartitionKey = "space1",
                RowKey = "Tag1",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Tag2", new Dictionary<string, string> { { "V1", "V2" } } }
                }
            };

            var tag2 = new TagEntity
            {
                PartitionKey = "space1",
                RowKey = "Tag2",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Tag1", new Dictionary<string, string> { { "V2", "V1" } } }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.CreateAsync(tag));
        }
    }
}
