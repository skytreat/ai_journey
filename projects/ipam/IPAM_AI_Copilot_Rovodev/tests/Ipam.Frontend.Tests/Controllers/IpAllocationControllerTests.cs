using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Ipam.ServiceContract.Interfaces;
using Ipam.ServiceContract.DTOs;
using Ipam.Frontend.Controllers;
using Ipam.Frontend.Models;
using System.Threading.Tasks;
using Ipam.Frontend.Tests.TestHelpers;

namespace Ipam.Frontend.Tests.Controllers
{
    public class IpAllocationControllerTests : ControllerTestBase<IpAllocationController>
    {
        private Mock<IIpAllocationService>? _ipAllocationServiceMock;

        protected override IpAllocationController CreateController()
        {
            _ipAllocationServiceMock = new Mock<IIpAllocationService>();
            return new IpAllocationController(_ipAllocationServiceMock.Object);
        }

        [Fact]
        public async Task GetById_ExistingIpNode_ReturnsOkResult()
        {
            // Arrange
            var ipAllocation = new IpAllocation { Id = "ip1", AddressSpaceId = "space1" };
            _ipAllocationServiceMock!.Setup(x => x.GetIpAllocationByIdAsync("space1", "ip1", CancellationToken.None))
                .ReturnsAsync(ipAllocation);

            // Act
            var result = await Controller.GetById("space1", "ip1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(ipAllocation, okResult.Value);
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
            var ipAllocation = new IpAllocation { Id = "ip1", AddressSpaceId = "space1", Prefix = "10.0.0.0/8" };
            _ipAllocationServiceMock!.Setup(x => x.CreateIpAllocationAsync(It.Is<IpAllocation>(a => a.AddressSpaceId == "space1" && a.Prefix == "10.0.0.0/8"), CancellationToken.None))
                .ReturnsAsync(ipAllocation);

            // Act
            var result = await Controller.Create("space1", model);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(ipAllocation, createdResult.Value);
        }
    }
}