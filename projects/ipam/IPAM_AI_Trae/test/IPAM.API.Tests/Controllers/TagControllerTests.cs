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
    public class TagControllerTests
    {
        private readonly Mock<IRepository> _mockRepository;
        private readonly TagController _controller;

        public TagControllerTests()
        {
            _mockRepository = new Mock<IRepository>();
            _controller = new TagController(_mockRepository.Object);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenTagDoesNotExist()
        {
            // Arrange
            var addressSpaceId = Guid.NewGuid();
            var testName = "nonexistent";
            _mockRepository.Setup(repo => repo.GetTagById(addressSpaceId, testName))
                .ReturnsAsync((Tag)null);

            // Act
            var result = await _controller.GetById(addressSpaceId, testName);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsTag_WhenTagExists()
        {
            // Arrange
            var addressSpaceId = Guid.NewGuid();
            var testName = "test";
            var testTag = new Tag { Name = testName, AddressSpaceId = addressSpaceId };
            _mockRepository.Setup(repo => repo.GetTagById(addressSpaceId, testName))
                .ReturnsAsync(testTag);

            // Act
            var result = await _controller.GetById(addressSpaceId, testName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<Tag>(okResult.Value);
            Assert.Equal(testName, returnValue.Name);
            Assert.Equal(addressSpaceId, returnValue.AddressSpaceId);
        }
    }
}