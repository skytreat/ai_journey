using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Interfaces;
using Ipam.Frontend.Controllers;
using Ipam.Frontend.Models;
using System.Threading.Tasks;

namespace Ipam.Frontend.Tests.Controllers
{
    public class IpNodeControllerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly IpNodeController _controller;

        public IpNodeControllerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _controller = new IpNodeController(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Get_ExistingIpNode_ReturnsOkResult()
        {
            // Arrange
            var ipNode = new IpNode { PartitionKey = "space1", RowKey = "ip1" };
            _unitOfWorkMock.Setup(x => x.IpNodes.GetByIdAsync("space1", "ip1"))
                .ReturnsAsync(ipNode);

            // Act
            var result = await _controller.Get("space1", "ip1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(ipNode, okResult.Value);
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

            // Act
            var result = await _controller.Create(model);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
        }
    }
}
