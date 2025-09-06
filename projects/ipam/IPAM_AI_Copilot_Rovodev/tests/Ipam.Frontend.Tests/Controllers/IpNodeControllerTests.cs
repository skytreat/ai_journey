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
    public class IpNodeControllerTests
    {
        private readonly Mock<IDataAccessService> _dataServiceMock;
        private readonly IpNodeController _controller;

        public IpNodeControllerTests()
        {
            _dataServiceMock = new Mock<IDataAccessService>();
            _controller = new IpNodeController(_dataServiceMock.Object);
        }

        [Fact]
        public async Task GetById_ExistingIpNode_ReturnsOkResult()
        {
            // Arrange
            var ipAddress = new IPAddress { Id = "ip1", AddressSpaceId = "space1" };
            _dataServiceMock.Setup(x => x.GetIPAddressAsync("space1", "ip1"))
                .ReturnsAsync(ipAddress);

            // Act
            var result = await _controller.GetById("space1", "ip1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(ipAddress, okResult.Value);
        }

        [Fact]
        public async Task Create_ValidModel_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var model = new IpNodeCreateModel
            {
                AddressSpaceId = "space1",
                Prefix = "10.0.0.0/8"
            };
            var ipAddress = new IPAddress { Id = "ip1", AddressSpaceId = "space1", Prefix = "10.0.0.0/8" };
            _dataServiceMock.Setup(x => x.CreateIPAddressAsync(It.IsAny<IPAddress>()))
                .ReturnsAsync(ipAddress);

            // Act
            var result = await _controller.Create("space1", model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(ipAddress, createdResult.Value);
        }
    }
}
