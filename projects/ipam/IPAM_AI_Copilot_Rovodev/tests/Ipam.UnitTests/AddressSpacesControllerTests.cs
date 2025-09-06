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
    public class AddressSpacesControllerTests
    {
        [Fact]
        public async Task CreateAddressSpace_WithValidAddressSpace_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            var addressSpace = new AddressSpace
            {
                Id = "test-id",
                Name = "Test Address Space",
                Description = "Test Description"
            };
            
            mockDataAccessService.Setup(service => service.CreateAddressSpaceAsync(addressSpace))
                .ReturnsAsync(addressSpace);

            // Act
            var result = await controller.CreateAddressSpace(addressSpace);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnValue = Assert.IsType<AddressSpace>(createdAtActionResult.Value);
            Assert.Equal(addressSpace.Id, returnValue.Id);
            mockDataAccessService.Verify(service => service.CreateAddressSpaceAsync(addressSpace), Times.Once);
        }
        
        [Fact]
        public async Task CreateAddressSpace_WithNullAddressSpace_ReturnsBadRequest()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            
            // Act
            var result = await controller.CreateAddressSpace(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid address space data.", badRequestResult.Value);
        }
        
        [Fact]
        public async Task GetAddressSpace_WithExistingId_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            var addressSpace = new AddressSpace
            {
                Id = "test-id",
                Name = "Test Address Space",
                Description = "Test Description"
            };
            
            mockDataAccessService.Setup(service => service.GetAddressSpaceAsync("test-id"))
                .ReturnsAsync(addressSpace);

            // Act
            var result = await controller.GetAddressSpace("test-id");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<AddressSpace>(okResult.Value);
            Assert.Equal(addressSpace.Id, returnValue.Id);
            mockDataAccessService.Verify(service => service.GetAddressSpaceAsync("test-id"), Times.Once);
        }
        
        [Fact]
        public async Task GetAddressSpace_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            
            mockDataAccessService.Setup(service => service.GetAddressSpaceAsync("non-existing-id"))
                .ReturnsAsync((AddressSpace)null);

            // Act
            var result = await controller.GetAddressSpace("non-existing-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            mockDataAccessService.Verify(service => service.GetAddressSpaceAsync("non-existing-id"), Times.Once);
        }
        
        [Fact]
        public async Task GetAddressSpaces_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            var addressSpaces = new List<AddressSpace>
            {
                new AddressSpace { Id = "1", Name = "Address Space 1" },
                new AddressSpace { Id = "2", Name = "Address Space 2" }
            };
            
            mockDataAccessService.Setup(service => service.GetAddressSpacesAsync())
                .ReturnsAsync(addressSpaces);

            // Act
            var result = await controller.GetAddressSpaces();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<AddressSpace>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            mockDataAccessService.Verify(service => service.GetAddressSpacesAsync(), Times.Once);
        }
        
        [Fact]
        public async Task UpdateAddressSpace_WithValidAddressSpace_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            var addressSpace = new AddressSpace
            {
                Id = "test-id",
                Name = "Updated Address Space",
                Description = "Updated Description"
            };
            
            mockDataAccessService.Setup(service => service.UpdateAddressSpaceAsync(addressSpace))
                .ReturnsAsync(addressSpace);

            // Act
            var result = await controller.UpdateAddressSpace("test-id", addressSpace);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<AddressSpace>(okResult.Value);
            Assert.Equal(addressSpace.Id, returnValue.Id);
            mockDataAccessService.Verify(service => service.UpdateAddressSpaceAsync(addressSpace), Times.Once);
        }
        
        [Fact]
        public async Task UpdateAddressSpace_WithNullAddressSpace_ReturnsBadRequest()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            
            // Act
            var result = await controller.UpdateAddressSpace("test-id", null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Address space ID mismatch.", badRequestResult.Value);
        }
        
        [Fact]
        public async Task UpdateAddressSpace_WithIdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            var addressSpace = new AddressSpace
            {
                Id = "different-id",
                Name = "Updated Address Space",
                Description = "Updated Description"
            };
            
            // Act
            var result = await controller.UpdateAddressSpace("test-id", addressSpace);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Address space ID mismatch.", badRequestResult.Value);
        }
        
        [Fact]
        public async Task DeleteAddressSpace_ReturnsNoContent()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new AddressSpacesController(mockDataAccessService.Object);
            
            mockDataAccessService.Setup(service => service.DeleteAddressSpaceAsync("test-id"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await controller.DeleteAddressSpace("test-id");

            // Assert
            Assert.IsType<NoContentResult>(result);
            mockDataAccessService.Verify(service => service.DeleteAddressSpaceAsync("test-id"), Times.Once);
        }
    }
}