using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Ipam.ServiceContract.Interfaces;
using Ipam.ServiceContract.DTOs;
using Ipam.Frontend.Controllers;
using Ipam.Frontend.Models;
using System.Threading.Tasks;

namespace Ipam.Frontend.Tests.Controllers
{
    public class AddressSpacesControllerTests
    {
        private readonly Mock<IAddressSpaceService> _addressSpaceServiceMock;
        private readonly AddressSpacesController _controller;

        public AddressSpacesControllerTests()
        {
            _addressSpaceServiceMock = new Mock<IAddressSpaceService>();
            _controller = new AddressSpacesController(_addressSpaceServiceMock.Object);
        }

        [Fact]
        public async Task CreateAddressSpace_ValidModel_ReturnsCreatedResult()
        {
            // Arrange
            var model = new AddressSpaceCreateModel { Name = "Test Space" };
            var addressSpace = new AddressSpace { Id = "1", Name = "Test Space" };
            _addressSpaceServiceMock.Setup(x => x.CreateAddressSpaceAsync(It.IsAny<AddressSpace>(), CancellationToken.None))
                .ReturnsAsync(addressSpace);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(addressSpace, createdResult.Value);
        }

        [Fact]
        public async Task GetAddressSpace_ExistingId_ReturnsOkResult()
        {
            // Arrange
            var addressSpace = new AddressSpace { Id = "1" };
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpaceByIdAsync("1", CancellationToken.None))
                .ReturnsAsync(addressSpace);

            // Act
            var result = await _controller.GetById("1");

            // Assert
            var actionResult = Assert.IsType<ActionResult<AddressSpace>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(addressSpace, okResult.Value);
        }
    }
}