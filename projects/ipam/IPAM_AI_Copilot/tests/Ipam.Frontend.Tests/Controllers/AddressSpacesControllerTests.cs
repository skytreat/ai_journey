using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Ipam.Frontend.Controllers;
using Ipam.Frontend.Models;
using System.Threading.Tasks;

namespace Ipam.Frontend.Tests.Controllers
{
    public class AddressSpacesControllerTests
    {
        private readonly Mock<IDataAccessService> _dataServiceMock;
        private readonly AddressSpacesController _controller;

        public AddressSpacesControllerTests()
        {
            _dataServiceMock = new Mock<IDataAccessService>();
            _controller = new AddressSpacesController(_dataServiceMock.Object);
        }

        [Fact]
        public async Task CreateAddressSpace_ValidModel_ReturnsCreatedResult()
        {
            // Arrange
            var model = new AddressSpaceCreateModel { Name = "Test Space" };
            var addressSpace = new AddressSpace { Id = "1", Name = "Test Space" };
            _dataServiceMock.Setup(x => x.CreateAddressSpaceAsync(It.IsAny<AddressSpace>()))
                .ReturnsAsync(addressSpace);

            // Act
            var result = await _controller.CreateAddressSpace(model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(addressSpace, createdResult.Value);
        }

        [Fact]
        public async Task GetAddressSpace_ExistingId_ReturnsOkResult()
        {
            // Arrange
            var addressSpace = new AddressSpace { Id = "1" };
            _dataServiceMock.Setup(x => x.GetAddressSpaceAsync("1"))
                .ReturnsAsync(addressSpace);

            // Act
            var result = await _controller.GetAddressSpace("1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(addressSpace, okResult.Value);
        }
    }
}
