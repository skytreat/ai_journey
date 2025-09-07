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
    public class IpNodeControllerTests
    {
        [Fact]
        public async Task CreateIPAddress_WithValidIpAllocation_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IpNodeController(mockDataAccessService.Object);
            var ipAllocation = new IpAllocation
            {
                Id = "192.168.1.1",
                Prefix = "192.168.1.0/24",
                AddressSpaceId = "default",
                ParentId = null
            };
            
            mockDataAccessService.Setup(service => service.CreateIPAddressAsync(ipAllocation))
                .ReturnsAsync(ipAllocation);

            // Act
            var result = await controller.CreateIPAddress(ipAllocation);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnValue = Assert.IsType<IpAllocation>(createdAtActionResult.Value);
            Assert.Equal(ipAllocation.Id, returnValue.Id);
            mockDataAccessService.Verify(service => service.CreateIPAddressAsync(ipAllocation), Times.Once);
        }
        
        [Fact]
        public async Task CreateIPAddress_WithNullIpAllocation_ReturnsBadRequest()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IpNodeController(mockDataAccessService.Object);
            
            // Act
            var result = await controller.CreateIPAddress(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid IP allocation data.", badRequestResult.Value);
        }
        
        [Fact]
        public async Task GetIPAddress_WithExistingId_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IpNodeController(mockDataAccessService.Object);
            var ipAllocation = new IpAllocation
            {
                Id = "192.168.1.1",
                Prefix = "192.168.1.0/24",
                AddressSpaceId = "default",
                ParentId = null
            };
            
            mockDataAccessService.Setup(service => service.GetIPAddressAsync("default", "192.168.1.1"))
                .ReturnsAsync(ipAllocation);

            // Act
            var result = await controller.GetIPAddress("default", "192.168.1.1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<IpAllocation>(okResult.Value);
            Assert.Equal(ipAllocation.Id, returnValue.Id);
            mockDataAccessService.Verify(service => service.GetIPAddressAsync("default", "192.168.1.1"), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddress_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IpNodeController(mockDataAccessService.Object);
            
            mockDataAccessService.Setup(service => service.GetIPAddressAsync("default", "192.168.1.2"))
                .ReturnsAsync((IpAllocation)null);

            // Act
            var result = await controller.GetIPAddress("default", "192.168.1.2");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            mockDataAccessService.Verify(service => service.GetIPAddressAsync("default", "192.168.1.2"), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddresses_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IpNodeController(mockDataAccessService.Object);
            var ipAllocations = new List<IpAllocation>
            {
                new IpAllocation { Id = "192.168.1.1", Prefix = "192.168.1.0/24" },
                new IpAllocation { Id = "192.168.1.2", Prefix = "192.168.1.0/24" }
            };
            
            mockDataAccessService.Setup(service => service.GetIPAddressesAsync("default", null, null))
                .ReturnsAsync(ipAllocations);

            // Act
            var result = await controller.GetIPAddresses("default");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<IpAllocation>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            mockDataAccessService.Verify(service => service.GetIPAddressesAsync("default", null, null), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddresses_WithCIDRFilter_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IpNodeController(mockDataAccessService.Object);
            var ipAllocations = new List<IpAllocation>
            {
                new IpAllocation { Id = "192.168.1.10", Prefix = "192.168.1.0/24" }
            };
            
            mockDataAccessService.Setup(service => service.GetIPAddressesAsync("default", "192.168.1.0/24", null))
                .ReturnsAsync(ipAllocations);

            // Act
            var result = await controller.GetIPAddresses("default", "192.168.1.0/24");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<IpAllocation>>(okResult.Value);
            Assert.Single(returnValue);
            mockDataAccessService.Verify(service => service.GetIPAddressesAsync("default", "192.168.1.0/24", null), Times.Once);
        }
        
        [Fact]
        public async Task GetIPAddresses_WithTagsFilter_ReturnsOkResult()
        {
            // Arrange
            var mockDataAccessService = new Mock<IDataAccessService>();
            var controller = new IpNodeController(mockDataAccessService.Object);
            var ipAllocations = new List<IpAllocation>
            {
                new IpAllocation { Id = "192.168.1.20", Prefix = "192.168.1.0/24" }
            };
            var tags = new Dictionary<string, string> { { "Environment", "Production" } };
            
            mockDataAccessService.Setup(service => service.GetIPAddressesAsync("default", null, tags))
                .ReturnsAsync(ipAllocations);

            // Act
            var result = await controller.GetIPAddresses("default", null, tags);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<IpAllocation>>(okResult.Value);
            Assert.Single(returnValue);
            mockDataAccessService.Verify(service => service.GetIPAddressesAsync("default", null, tags), Times.Once);
        }
    }
}