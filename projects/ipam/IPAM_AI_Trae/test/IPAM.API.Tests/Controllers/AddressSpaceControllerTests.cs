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
    public class AddressSpaceControllerTests
    {
        private readonly Mock<IRepository> _mockRepository;
        private readonly AddressSpaceController _controller;

        public AddressSpaceControllerTests()
        {
            _mockRepository = new Mock<IRepository>();
            _controller = new AddressSpaceController(_mockRepository.Object);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenAddressSpaceDoesNotExist()
        {
            // Arrange
            var testId = Guid.NewGuid();
            _mockRepository.Setup(repo => repo.GetAddressSpaceById(testId))
                .ReturnsAsync((AddressSpace)null);

            // Act
            var result = await _controller.GetById(testId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsAddressSpace_WhenAddressSpaceExists()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var testAddressSpace = new AddressSpace { Id = testId, Name = "Test" };
            _mockRepository.Setup(repo => repo.GetAddressSpaceById(testId))
                .ReturnsAsync(testAddressSpace);

            // Act
            var result = await _controller.GetById(testId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AddressSpace>(okResult.Value);
            Assert.Equal(testId, returnValue.Id);
            Assert.Equal("Test", returnValue.Name);
        }
    }
}