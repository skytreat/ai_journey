using IPAM.Core;
using IPAM.Data;
using IPAM.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace IPAM.API.Tests.Controllers
{
    public class IPControllerTests
    {
        private readonly Mock<IRepository> _mockRepository;
        private readonly IPController _controller;

        public IPControllerTests()
        {
            _mockRepository = new Mock<IRepository>();
            _controller = new IPController(_mockRepository.Object);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenIPDoesNotExist()
        {
            // Arrange
            var addressSpaceId = Guid.NewGuid();
            var testId = Guid.NewGuid();
            _mockRepository.Setup(repo => repo.GetIpById(addressSpaceId, testId))
                .ReturnsAsync((IP)null);

            // Act
            var result = await _controller.GetById(addressSpaceId, testId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsIP_WhenIPExists()
        {
            // Arrange
            var addressSpaceId = Guid.NewGuid();
            var testId = Guid.NewGuid();
            var testIP = new IP { Id = testId, AddressSpaceId = addressSpaceId, Prefix = "192.168.1.0/24" };
            _mockRepository.Setup(repo => repo.GetIpById(addressSpaceId, testId))
                .ReturnsAsync(testIP);

            // Act
            var result = await _controller.GetById(addressSpaceId, testId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<IP>(okResult.Value);
            Assert.Equal(testId, returnValue.Id);
            Assert.Equal(addressSpaceId, returnValue.AddressSpaceId);
            Assert.Equal("192.168.1.0/24", returnValue.Prefix);
        }
    }
}