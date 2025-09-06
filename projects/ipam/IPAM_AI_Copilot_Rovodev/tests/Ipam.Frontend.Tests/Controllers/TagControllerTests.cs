using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Ipam.Frontend.Controllers;
using Ipam.Frontend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Ipam.Frontend.Tests.Controllers
{
    /// <summary>
    /// Comprehensive unit tests for TagController
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagControllerTests
    {
        private readonly Mock<IDataAccessService> _dataServiceMock;
        private readonly TagController _controller;

        public TagControllerTests()
        {
            _dataServiceMock = new Mock<IDataAccessService>();
            _controller = new TagController(_dataServiceMock.Object);

            // Setup user context for authorization tests
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "AddressSpaceAdmin")
            }));
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetById_ExistingTag_ReturnsOkWithTag()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tagName = "Environment";
            var expectedTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName,
                Type = "Inheritable",
                Description = "Environment classification"
            };

            _dataServiceMock.Setup(x => x.GetTagAsync(addressSpaceId, tagName))
                .ReturnsAsync(expectedTag);

            // Act
            var result = await _controller.GetById(addressSpaceId, tagName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tag = Assert.IsType<Tag>(okResult.Value);
            Assert.Equal(expectedTag.Name, tag.Name);
            Assert.Equal(expectedTag.Type, tag.Type);
        }

        [Fact]
        public async Task GetById_NonExistentTag_ReturnsNotFound()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tagName = "NonExistent";

            _dataServiceMock.Setup(x => x.GetTagAsync(addressSpaceId, tagName))
                .ReturnsAsync((Tag)null);

            // Act
            var result = await _controller.GetById(addressSpaceId, tagName);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetAll_ValidAddressSpace_ReturnsOkWithTags()
        {
            // Arrange
            var addressSpaceId = "space1";
            var expectedTags = new List<Tag>
            {
                new Tag { Name = "Environment", Type = "Inheritable" },
                new Tag { Name = "Application", Type = "NonInheritable" },
                new Tag { Name = "Region", Type = "Inheritable" }
            };

            _dataServiceMock.Setup(x => x.GetTagsAsync(addressSpaceId))
                .ReturnsAsync(expectedTags);

            // Act
            var result = await _controller.GetAll(addressSpaceId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tags = Assert.IsAssignableFrom<IEnumerable<Tag>>(okResult.Value);
            Assert.Equal(3, ((List<Tag>)tags).Count);
        }

        [Fact]
        public async Task Create_ValidTag_ReturnsCreatedResult()
        {
            // Arrange
            var addressSpaceId = "space1";
            var model = new TagCreateModel
            {
                AddressSpaceId = addressSpaceId,
                Name = "Environment",
                Type = "Inheritable",
                Description = "Environment classification",
                KnownValues = new[] { "Production", "Development", "Testing" }
            };

            var createdTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = model.Name,
                Type = model.Type,
                Description = model.Description,
                KnownValues = model.KnownValues,
                CreatedOn = DateTime.UtcNow
            };

            _dataServiceMock.Setup(x => x.CreateTagAsync(addressSpaceId, It.IsAny<Tag>()))
                .ReturnsAsync(createdTag);

            // Act
            var result = await _controller.Create(addressSpaceId, model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
            Assert.Equal(createdTag, createdResult.Value);
            
            var routeValues = createdResult.RouteValues;
            Assert.Equal(addressSpaceId, routeValues["addressSpaceId"]);
            Assert.Equal(model.Name, routeValues["tagName"]);
        }

        [Fact]
        public async Task Create_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var addressSpaceId = "space1";
            var model = new TagCreateModel(); // Invalid - missing required fields
            
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.Create(addressSpaceId, model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(_controller.ModelState, badRequestResult.Value);
        }

        [Fact]
        public async Task Create_AddressSpaceIdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var addressSpaceId = "space1";
            var model = new TagCreateModel
            {
                AddressSpaceId = "different-space", // Mismatch
                Name = "Environment",
                Type = "Inheritable"
            };

            // Act
            var result = await _controller.Create(addressSpaceId, model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("mismatch", badRequestResult.Value.ToString().ToLower());
        }

        [Fact]
        public async Task Create_WithImplications_CreatesTagWithImplications()
        {
            // Arrange
            var addressSpaceId = "space1";
            var model = new TagCreateModel
            {
                AddressSpaceId = addressSpaceId,
                Name = "Datacenter",
                Type = "Inheritable",
                Description = "Datacenter location",
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Datacenter", new Dictionary<string, string> { { "AMS05", "Region=EuropeWest" } } }
                }
            };

            var createdTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = model.Name,
                Type = model.Type,
                Implies = model.Implies
            };

            _dataServiceMock.Setup(x => x.CreateTagAsync(addressSpaceId, It.IsAny<Tag>()))
                .ReturnsAsync(createdTag);

            // Act
            var result = await _controller.Create(addressSpaceId, model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var tag = Assert.IsType<Tag>(createdResult.Value);
            Assert.NotNull(tag.Implies);
            Assert.Contains("Datacenter", tag.Implies.Keys);
        }

        [Fact]
        public async Task Update_ExistingTag_ReturnsOkWithUpdatedTag()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tagName = "Environment";
            var model = new TagCreateModel
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName,
                Type = "Inheritable",
                Description = "Updated description",
                KnownValues = new[] { "Production", "Development", "Testing", "Staging" }
            };

            var existingTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName,
                Type = "Inheritable",
                Description = "Original description",
                CreatedOn = DateTime.UtcNow.AddDays(-1)
            };

            var updatedTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName,
                Type = model.Type,
                Description = model.Description,
                KnownValues = model.KnownValues,
                ModifiedOn = DateTime.UtcNow
            };

            _dataServiceMock.Setup(x => x.GetTagAsync(addressSpaceId, tagName))
                .ReturnsAsync(existingTag);
            _dataServiceMock.Setup(x => x.UpdateTagAsync(addressSpaceId, It.IsAny<Tag>()))
                .ReturnsAsync(updatedTag);

            // Act
            var result = await _controller.Update(addressSpaceId, tagName, model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tag = Assert.IsType<Tag>(okResult.Value);
            Assert.Equal(model.Description, tag.Description);
            Assert.Equal(4, tag.KnownValues.Length);
        }

        [Fact]
        public async Task Update_NonExistentTag_ReturnsNotFound()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tagName = "NonExistent";
            var model = new TagCreateModel
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName,
                Type = "Inheritable"
            };

            _dataServiceMock.Setup(x => x.GetTagAsync(addressSpaceId, tagName))
                .ReturnsAsync((Tag)null);

            // Act
            var result = await _controller.Update(addressSpaceId, tagName, model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ExistingTag_ReturnsNoContent()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tagName = "Environment";
            var existingTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName
            };

            _dataServiceMock.Setup(x => x.GetTagAsync(addressSpaceId, tagName))
                .ReturnsAsync(existingTag);

            // Act
            var result = await _controller.Delete(addressSpaceId, tagName);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _dataServiceMock.Verify(x => x.DeleteTagAsync(addressSpaceId, tagName), Times.Once);
        }

        [Fact]
        public async Task Delete_NonExistentTag_ReturnsNotFound()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tagName = "NonExistent";

            _dataServiceMock.Setup(x => x.GetTagAsync(addressSpaceId, tagName))
                .ReturnsAsync((Tag)null);

            // Act
            var result = await _controller.Delete(addressSpaceId, tagName);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _dataServiceMock.Verify(x => x.DeleteTagAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("Inheritable")]
        [InlineData("NonInheritable")]
        public async Task Create_ValidTagTypes_AcceptsBothTypes(string tagType)
        {
            // Arrange
            var addressSpaceId = "space1";
            var model = new TagCreateModel
            {
                AddressSpaceId = addressSpaceId,
                Name = "TestTag",
                Type = tagType,
                Description = "Test tag"
            };

            var createdTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = model.Name,
                Type = model.Type
            };

            _dataServiceMock.Setup(x => x.CreateTagAsync(addressSpaceId, It.IsAny<Tag>()))
                .ReturnsAsync(createdTag);

            // Act
            var result = await _controller.Create(addressSpaceId, model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var tag = Assert.IsType<Tag>(createdResult.Value);
            Assert.Equal(tagType, tag.Type);
        }

        [Fact]
        public async Task Create_WithAttributes_CreatesTagWithAttributes()
        {
            // Arrange
            var addressSpaceId = "space1";
            var model = new TagCreateModel
            {
                AddressSpaceId = addressSpaceId,
                Name = "Environment",
                Type = "Inheritable",
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Validation", new Dictionary<string, string> { { "Required", "true" } } },
                    { "Display", new Dictionary<string, string> { { "Color", "blue" } } }
                }
            };

            var createdTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = model.Name,
                Attributes = model.Attributes
            };

            _dataServiceMock.Setup(x => x.CreateTagAsync(addressSpaceId, It.IsAny<Tag>()))
                .ReturnsAsync(createdTag);

            // Act
            var result = await _controller.Create(addressSpaceId, model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var tag = Assert.IsType<Tag>(createdResult.Value);
            Assert.NotNull(tag.Attributes);
            Assert.Equal(2, tag.Attributes.Count);
            Assert.Contains("Validation", tag.Attributes.Keys);
            Assert.Contains("Display", tag.Attributes.Keys);
        }

        [Fact]
        public async Task GetAll_EmptyAddressSpace_ReturnsEmptyList()
        {
            // Arrange
            var addressSpaceId = "empty-space";
            var emptyTags = new List<Tag>();

            _dataServiceMock.Setup(x => x.GetTagsAsync(addressSpaceId))
                .ReturnsAsync(emptyTags);

            // Act
            var result = await _controller.GetAll(addressSpaceId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tags = Assert.IsAssignableFrom<IEnumerable<Tag>>(okResult.Value);
            Assert.Empty(tags);
        }

        [Fact]
        public async Task Update_ModifiesTimestamp_SetsModifiedOn()
        {
            // Arrange
            var addressSpaceId = "space1";
            var tagName = "Environment";
            var model = new TagCreateModel
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName,
                Type = "Inheritable",
                Description = "Updated"
            };

            var existingTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = tagName,
                CreatedOn = DateTime.UtcNow.AddDays(-1),
                ModifiedOn = DateTime.UtcNow.AddDays(-1)
            };

            _dataServiceMock.Setup(x => x.GetTagAsync(addressSpaceId, tagName))
                .ReturnsAsync(existingTag);

            // Capture the tag passed to UpdateTagAsync to verify ModifiedOn is set
            Tag capturedTag = null;
            _dataServiceMock.Setup(x => x.UpdateTagAsync(addressSpaceId, It.IsAny<Tag>()))
                .Callback<string, Tag>((id, tag) => capturedTag = tag)
                .ReturnsAsync(existingTag);

            // Act
            await _controller.Update(addressSpaceId, tagName, model);

            // Assert
            Assert.NotNull(capturedTag);
            Assert.True(capturedTag.ModifiedOn > existingTag.CreatedOn);
        }
    }
}