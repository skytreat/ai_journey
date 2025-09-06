using Xunit;
using Moq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
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
                .ReturnsAsync(new Tag { Type = "NonInheritable" });

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

            var datacenterTag = new Tag
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

            var datacenterTag = new Tag
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
                .ReturnsAsync(new Tag { Type = "NonInheritable" });

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
                .ReturnsAsync(new Tag { Type = "Inheritable" });

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
                .ReturnsAsync(new Tag { Type = "Inheritable" });
            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Application"))
                .ReturnsAsync(new Tag { Type = "NonInheritable" });

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
                .ReturnsAsync(new Tag { Type = "Inheritable" });

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

            var children = new List<IpNode>
            {
                new IpNode { Tags = new Dictionary<string, string>() },
                new IpNode { Tags = new Dictionary<string, string> { { "Region", "USEast" } } }
            };

            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Environment"))
                .ReturnsAsync(new Tag { Type = "Inheritable" });
            _tagRepositoryMock.Setup(x => x.GetByNameAsync("space1", "Application"))
                .ReturnsAsync(new Tag { Type = "NonInheritable" });

            // Act
            await _service.PropagateTagsToChildren("space1", parentTags, children);

            // Assert
            Assert.Equal("Production", children[0].Tags["Environment"]);
            Assert.False(children[0].Tags.ContainsKey("Application"));
            Assert.Equal("Production", children[1].Tags["Environment"]);
            Assert.Equal("USEast", children[1].Tags["Region"]);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, new string[0])]
        public async Task PropagateTagsToChildren_NullOrEmptyInputs_DoesNotThrow(
            Dictionary<string, string> parentTags, 
            IpNode[] children)
        {
            // Act & Assert
            await _service.PropagateTagsToChildren("space1", parentTags, children ?? new IpNode[0]);
        }
    }
}