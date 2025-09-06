using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Ipam.Frontend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Ipam.UnitTests
{
    public class TagsControllerTests
    {
        [Fact]
        public async Task CreateTag_WithValidTag_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            var tag = new Tag
            {
                Name = "Environment",
                Description = "Environment tag",
                Type = TagType.Inheritable,
                KnownValues = new List<string> { "Production", "Development" }
            };
            
            mockDataAccessService.Setup(service => service.CreateTagAsync("test-address-space", tag))
                .ReturnsAsync(tag);

            // Act
            var result = await controller.CreateTag("test-address-space", tag);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnValue = Assert.IsType<Tag>(createdAtActionResult.Value);
            Assert.Equal(tag.Name, returnValue.Name);
            mockDataAccessService.Verify(service => service.CreateTagAsync("test-address-space", tag), Times.Once);
        }
        
        [Fact]
        public async Task CreateTag_WithNullTag_ReturnsBadRequest()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            
            // Act
            var result = await controller.CreateTag("test-address-space", null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid tag data.", badRequestResult.Value);
        }
        
        [Fact]
        public async Task GetTag_WithExistingTag_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            var tag = new Tag
            {
                Name = "Environment",
                Description = "Environment tag",
                Type = TagType.Inheritable,
                KnownValues = new List<string> { "Production", "Development" }
            };
            
            mockDataAccessService.Setup(service => service.GetTagAsync("test-address-space", "Environment"))
                .ReturnsAsync(tag);

            // Act
            var result = await controller.GetTag("test-address-space", "Environment");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<Tag>(okResult.Value);
            Assert.Equal(tag.Name, returnValue.Name);
            mockDataAccessService.Verify(service => service.GetTagAsync("test-address-space", "Environment"), Times.Once);
        }
        
        [Fact]
        public async Task GetTag_WithNonExistingTag_ReturnsNotFound()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            
            mockDataAccessService.Setup(service => service.GetTagAsync("test-address-space", "NonExisting"))
                .ReturnsAsync((Tag)null);

            // Act
            var result = await controller.GetTag("test-address-space", "NonExisting");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            mockDataAccessService.Verify(service => service.GetTagAsync("test-address-space", "NonExisting"), Times.Once);
        }
        
        [Fact]
        public async Task GetTags_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            var tags = new List<Tag>
            {
                new Tag { Name = "Environment", Type = TagType.Inheritable },
                new Tag { Name = "Owner", Type = TagType.NonInheritable }
            };
            
            mockDataAccessService.Setup(service => service.GetTagsAsync("test-address-space"))
                .ReturnsAsync(tags);

            // Act
            var result = await controller.GetTags("test-address-space");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<Tag>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            mockDataAccessService.Verify(service => service.GetTagsAsync("test-address-space"), Times.Once);
        }
        
        [Fact]
        public async Task UpdateTag_WithValidTag_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            var tag = new Tag
            {
                Name = "Environment",
                Description = "Updated environment tag",
                Type = TagType.Inheritable,
                KnownValues = new List<string> { "Production", "Development", "Test" }
            };
            
            mockDataAccessService.Setup(service => service.UpdateTagAsync("test-address-space", tag))
                .ReturnsAsync(tag);

            // Act
            var result = await controller.UpdateTag("test-address-space", "Environment", tag);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<Tag>(okResult.Value);
            Assert.Equal(tag.Name, returnValue.Name);
            mockDataAccessService.Verify(service => service.UpdateTagAsync("test-address-space", tag), Times.Once);
        }
        
        [Fact]
        public async Task UpdateTag_WithNullTag_ReturnsBadRequest()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            
            // Act
            var result = await controller.UpdateTag("test-address-space", "Environment", null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Tag name mismatch.", badRequestResult.Value);
        }
        
        [Fact]
        public async Task UpdateTag_WithNameMismatch_ReturnsBadRequest()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            var tag = new Tag
            {
                Name = "DifferentName",
                Description = "Updated environment tag",
                Type = TagType.Inheritable
            };
            
            // Act
            var result = await controller.UpdateTag("test-address-space", "Environment", tag);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Tag name mismatch.", badRequestResult.Value);
        }
        
        [Fact]
        public async Task DeleteTag_ReturnsNoContent()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new TagsController(mockDataAccessService.Object);
            
            mockDataAccessService.Setup(service => service.DeleteTagAsync("test-address-space", "Environment"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await controller.DeleteTag("test-address-space", "Environment");

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockDataAccessService.Verify(service => service.DeleteTagAsync("test-address-space", "Environment"), Times.Once);
        }
    }
}