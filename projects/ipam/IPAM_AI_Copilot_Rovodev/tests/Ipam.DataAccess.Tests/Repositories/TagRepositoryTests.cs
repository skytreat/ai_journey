using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ipam.DataAccess.Tests.TestHelpers;

namespace Ipam.DataAccess.Tests.Repositories
{
    public class TagRepositoryTests : RepositoryTestBase<TagRepository, TagEntity>
    {
        protected override TagRepository CreateRepository()
        {
            return new TagRepository(ConfigMock.Object);
        }

        protected override TagEntity CreateTestEntity()
        {
            return TestDataBuilders.CreateTestTagEntity();
        }

        [Fact]
        public async Task CreateAsync_ValidTag_ShouldSucceed()
        {
            // Arrange
            var tag = TestDataBuilders.CreateTestTagEntity(
                TestConstants.DefaultAddressSpaceId,
                "Region",
                "Inheritable",
                new List<string> { "USEast", "USWest" }
            );

            // Act
            var result = await Repository.CreateAsync(tag);

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
            await Assert.ThrowsAsync<ArgumentException>(() => Repository.CreateAsync(tag));
        }
    }
}
