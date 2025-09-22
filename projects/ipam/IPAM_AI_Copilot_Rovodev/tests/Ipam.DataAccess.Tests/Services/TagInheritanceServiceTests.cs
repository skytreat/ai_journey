using Xunit;
using Moq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Unit tests for TagInheritanceService
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagInheritanceServiceTests
    {
        private readonly Mock<ITagRepository> _tagRepositoryMock;
        private readonly TagInheritanceService _service;

        public TagInheritanceServiceTests()
        {
            _tagRepositoryMock = new Mock<ITagRepository>();    
            _service = new TagInheritanceService(_tagRepositoryMock.Object);
        }

        [Fact]
        public async Task ApplyTagImplications_NoImplications_ReturnsOriginalTags()
        {
            // Arrange
            var inputTags = new Dictionary<string, string>
            {
                { "Environment", "Production" }
            };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(new TagEntity { Type = "NonInheritable" });

            // Act
            var result = await _service.ApplyTagImplications("space1", inputTags);

            // Assert
            Assert.Equal(inputTags, result);
        }

        [Fact]
        public async Task ApplyTagImplications_WithImplications_AddsImpliedTags()
        {
            // Arrange
            var inputTags = new Dictionary<string, string>
            {
                { "Datacenter", "AMS05" }
            };

            var datacenterTag = new TagEntity
            {
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Datacenter", new Dictionary<string, string> { { "AMS05", "Region=EuropeWest" } } }
                }
            };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Datacenter"))
                .ReturnsAsync(datacenterTag);

            // Act
            var result = await _service.ApplyTagImplications("space1", inputTags);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("AMS05", result["Datacenter"]);
            Assert.Equal("EuropeWest", result["Region"]);
        }

        [Fact]
        public async Task ApplyTagImplications_ConflictingImplications_ThrowsException()
        {
            // Arrange
            var inputTags = new Dictionary<string, string>
            {
                { "Datacenter", "AMS05" },
                { "Region", "USEast" } // Conflicts with implied Region=EuropeWest
            };

            var datacenterTag = new TagEntity
            {
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Datacenter", new Dictionary<string, string> { { "AMS05", "Region=EuropeWest" } } }
                }
            };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Datacenter"))
                .ReturnsAsync(datacenterTag);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApplyTagImplications("space1", inputTags));
        }

        [Fact]
        public async Task ValidateTagInheritance_NoConflicts_DoesNotThrow()
        {
            // Arrange
            var parentTags = new Dictionary<string, string> { { "Environment", "Production" } };
            var childTags = new Dictionary<string, string> { { "Application", "WebServer" } };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(new TagEntity { Type = "NonInheritable" });

            // Act & Assert
            await _service.ValidateTagInheritance("space1", parentTags, childTags);
        }

        [Fact]
        public async Task ValidateTagInheritance_InheritableTagConflict_ThrowsException()
        {
            // Arrange
            var parentTags = new Dictionary<string, string> { { "Environment", "Production" } };
            var childTags = new Dictionary<string, string> { { "Environment", "Development" } };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(new TagEntity { Type = "Inheritable" });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ValidateTagInheritance("space1", parentTags, childTags));
        }

        [Fact]
        public async Task GetEffectiveTags_WithInheritableParentTags_IncludesParentTags()
        {
            // Arrange
            var nodeTags = new Dictionary<string, string> { { "Application", "WebServer" } };
            var parentTags = new Dictionary<string, string> { { "Environment", "Production" } };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(new TagEntity { Type = "Inheritable" });
            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Application"))
                .ReturnsAsync(new TagEntity { Type = "NonInheritable" });

            // Act
            var result = await _service.GetEffectiveTags("space1", nodeTags, parentTags);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Production", result["Environment"]);
            Assert.Equal("WebServer", result["Application"]);
        }

        [Fact]
        public async Task GetEffectiveTags_NodeTagOverridesParent_UsesNodeTag()
        {
            // Arrange
            var nodeTags = new Dictionary<string, string> { { "Environment", "Development" } };
            var parentTags = new Dictionary<string, string> { { "Environment", "Production" } };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(new TagEntity { Type = "Inheritable" });

            // Act
            var result = await _service.GetEffectiveTags("space1", nodeTags, parentTags);

            // Assert
            Assert.Single(result);
            Assert.Equal("Development", result["Environment"]);
        }

        [Fact]
        public async Task PropagateTagsToChildren_WithInheritableTags_AddsTagsToChildren()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Application", "WebServer" }
            };

            var children = new List<IpAllocationEntity>
            {
                new IpAllocationEntity { Tags = new Dictionary<string, string>() },
                new IpAllocationEntity { Tags = new Dictionary<string, string> { { "Region", "USEast" } } }
            };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(new TagEntity { Type = "Inheritable" });
            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Application"))
                .ReturnsAsync(new TagEntity { Type = "NonInheritable" });

            // Act
            await _service.PropagateTagsToChildren("space1", parentTags, children);

            // Assert
            Assert.Equal("Production", children[0].Tags["Environment"]);
            Assert.False(children[0].Tags.ContainsKey("Application"));
            Assert.Equal("Production", children[1].Tags["Environment"]);
            Assert.Equal("USEast", children[1].Tags["Region"]);
        }

        [Fact]
        public async Task PropagateTagsToChildren_NullOrEmptyInputs_DoesNotThrow()
        {
            // Act & Assert
            await _service.PropagateTagsToChildren("space1", null, new IpAllocationEntity[0]);
            await _service.PropagateTagsToChildren("space1", new Dictionary<string, string>(), null);
            await _service.PropagateTagsToChildren("space1", new Dictionary<string, string>(), new IpAllocationEntity[0]);
        }

        [Fact]
        public async Task ApplyTagImplications_ShouldNotImplyItself_WhenTagHasImplications()
        {
            // Arrange
            var mockTagRepository = new Mock<ITagRepository>();
            var service = new TagInheritanceService(mockTagRepository.Object);

            var addressSpaceId = "test-space";
            var inputTags = new Dictionary<string, string>
            {
                { "Datacenter", "AMS05" }
            };

            // Create a tag definition where Datacenter=AMS05 implies Region=EuropeWest
            var datacenterTag = new TagEntity
            {
                AddressSpaceId = addressSpaceId,
                Name = "Datacenter",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "Region", new Dictionary<string, string>
                        {
                            { "AMS05", "EuropeWest" }
                        }
                    }
                }
            };

            mockTagRepository.Setup(r => r.GetByNameAsync(addressSpaceId, "Datacenter"))
                           .ReturnsAsync(datacenterTag);

            // Act
            var result = await service.ApplyTagImplications(addressSpaceId, inputTags);

            // Assert
            Assert.Equal(2, result.Count); // Original tag + implied tag
            Assert.Equal("AMS05", result["Datacenter"]); // Original tag preserved
            Assert.Equal("EuropeWest", result["Region"]); // Implied tag added
        }

        [Fact]
        public async Task ApplyTagImplications_ShouldNotCrash_WhenTagTriesToImplyItself()
        {
            // Arrange
            var mockTagRepository = new Mock<ITagRepository>();
            var service = new TagInheritanceService(mockTagRepository.Object);

            var addressSpaceId = "test-space";
            var inputTags = new Dictionary<string, string>
            {
                { "Environment", "Prod" }
            };

            // Create a tag definition that incorrectly tries to imply itself
            var environmentTag = new TagEntity
            {
                AddressSpaceId = addressSpaceId,
                Name = "Environment",
                Type = "Inheritable",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    {
                        "Environment", new Dictionary<string, string>
                        {
                            { "Prod", "Production" }
                        }
                    }
                }
            };

            mockTagRepository.Setup(r => r.GetByNameAsync(addressSpaceId, "Environment"))
                           .ReturnsAsync(environmentTag);

            // Act
            var result = await service.ApplyTagImplications(addressSpaceId, inputTags);

            // Assert
            // Should not crash and should handle the self-implication gracefully
            Assert.Single(result); // Only the original tag should remain
            Assert.Equal("Prod", result["Environment"]); // Original value preserved
        }
    }
}