using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.Frontend.Controllers;
using Ipam.ServiceContract.Interfaces;
using Ipam.ServiceContract.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Ipam.ServiceContract.DTOs;
using Ipam.Frontend.Tests.TestHelpers;

namespace Ipam.Frontend.Tests.Controllers
{
    /// <summary>
    /// Unit tests for UtilizationController
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class UtilizationControllerTests : ControllerTestBase<UtilizationController>
    {
        private Mock<IIpAllocationService> _allocationServiceMock;
        private Mock<IPerformanceMonitoringService> _performanceServiceMock;
        private Mock<IAuditService> _auditServiceMock;

        protected override UtilizationController CreateController()
        {
            _allocationServiceMock = new Mock<IIpAllocationService>();
            _performanceServiceMock = new Mock<IPerformanceMonitoringService>();
            _auditServiceMock = new Mock<IAuditService>();

            var controller = new UtilizationController(
               _allocationServiceMock.Object,
               _performanceServiceMock.Object,
               _auditServiceMock.Object);

            // Setup user context
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser")
            }));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        [Fact]
        public async Task GetUtilization_ValidNetwork_ReturnsOkWithStats()
        {
            // Arrange
            var addressSpaceId = "space1";
            var networkCidr = "10.0.0.0/24";
            var expectedStats = new IpUtilizationStats
            {
                NetworkCidr = networkCidr,
                TotalAddresses = 256,
                AllocatedAddresses = 128,
                UtilizationPercentage = 50.0,
                SubnetCount = 2
            };

            _allocationServiceMock.Setup(x => x.CalculateUtilizationAsync(addressSpaceId, networkCidr, CancellationToken.None))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await Controller.GetUtilization(addressSpaceId, networkCidr);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var stats = Assert.IsType<IpUtilizationStats>(okResult.Value);
            Assert.Equal(expectedStats.NetworkCidr, stats.NetworkCidr);
            Assert.Equal(expectedStats.UtilizationPercentage, stats.UtilizationPercentage);
        }

        [Fact]
        public async Task GetUtilization_InvalidNetwork_ReturnsBadRequest()
        {
            // Arrange
            var addressSpaceId = "space1";
            var invalidCidr = "invalid-cidr";

            _allocationServiceMock.Setup(x => x.CalculateUtilizationAsync(addressSpaceId, invalidCidr, CancellationToken.None))
                .ThrowsAsync(new ArgumentException("Invalid CIDR"));

            // Act
            var result = await Controller.GetUtilization(addressSpaceId, invalidCidr);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid CIDR", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task FindAvailableSubnets_ValidRequest_ReturnsOkWithSubnets()
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentCidr = "10.0.0.0/16";
            var subnetSize = 24;
            var count = 5;
            var expectedSubnets = new List<string>
            {
                "10.0.1.0/24", "10.0.2.0/24", "10.0.3.0/24", "10.0.4.0/24", "10.0.5.0/24"
            };

            _allocationServiceMock.Setup(x => x.FindAvailableSubnetsAsync(addressSpaceId, parentCidr, subnetSize, count, CancellationToken.None))
                .ReturnsAsync(expectedSubnets);

            // Act
            var result = await Controller.FindAvailableSubnets(addressSpaceId, parentCidr, subnetSize, count);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(129)]
        public async Task FindAvailableSubnets_InvalidSubnetSize_ReturnsBadRequest(int invalidSubnetSize)
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentCidr = "10.0.0.0/16";
            var count = 5;

            // Act
            var result = await Controller.FindAvailableSubnets(addressSpaceId, parentCidr, invalidSubnetSize, count);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task FindAvailableSubnets_InvalidCount_ReturnsBadRequest(int invalidCount)
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentCidr = "10.0.0.0/16";
            var subnetSize = 24;

            // Act
            var result = await Controller.FindAvailableSubnets(addressSpaceId, parentCidr, subnetSize, invalidCount);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ValidateSubnetAllocation_ValidSubnet_ReturnsOkWithValidResult()
        {
            // Arrange
            var addressSpaceId = "space1";
            var request = new SubnetValidationRequest { ProposedCidr = "10.0.1.0/24" };
            var expectedResult = new SubnetValidationResult
            {
                IsValid = true,
                ProposedCidr = request.ProposedCidr,
                ConflictingSubnets = new List<string>(),
                ValidationMessage = "Subnet allocation is valid"
            };

            _allocationServiceMock.Setup(x => x.ValidateSubnetAllocationAsync(addressSpaceId, request.ProposedCidr, CancellationToken.None))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await Controller.ValidateSubnetAllocation(addressSpaceId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var validationResult = Assert.IsType<SubnetValidationResult>(okResult.Value);
            Assert.True(validationResult.IsValid);
            Assert.Equal(request.ProposedCidr, validationResult.ProposedCidr);
        }

        [Fact]
        public async Task ValidateSubnetAllocation_ConflictingSubnet_ReturnsOkWithInvalidResult()
        {
            // Arrange
            var addressSpaceId = "space1";
            var request = new SubnetValidationRequest { ProposedCidr = "10.0.1.0/24" };
            var expectedResult = new SubnetValidationResult
            {
                IsValid = false,
                ProposedCidr = request.ProposedCidr,
                ConflictingSubnets = new List<string> { "10.0.1.0/25" },
                ValidationMessage = "Subnet conflicts with existing allocations"
            };

            _allocationServiceMock.Setup(x => x.ValidateSubnetAllocationAsync(addressSpaceId, request.ProposedCidr, CancellationToken.None))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await Controller.ValidateSubnetAllocation(addressSpaceId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var validationResult = Assert.IsType<SubnetValidationResult>(okResult.Value);
            Assert.False(validationResult.IsValid);
            Assert.Single(validationResult.ConflictingSubnets);
        }

        [Fact]
        public async Task AllocateNextSubnet_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentCidr = "10.0.0.0/16";
            var request = new SubnetAllocationRequest
            {
                SubnetSize = 24,
                Tags = new Dictionary<string, string> { { "Environment", "Test" } }
            };

            var allocatedNode = new IpAllocation
            {
                Id = "allocated-id",
                Prefix = "10.0.1.0/24"
            };
            
            _allocationServiceMock.Setup(x => x.AllocateNextSubnetAsync(addressSpaceId, parentCidr, request.SubnetSize, request.Tags, CancellationToken.None))
                .ReturnsAsync(allocatedNode);

            // Act
            var result = await Controller.AllocateNextSubnet(addressSpaceId, parentCidr, request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(allocatedNode, createdResult.Value);
        }

        [Fact]
        public async Task AllocateNextSubnet_NoAvailableSubnets_ReturnsConflict()
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentCidr = "10.0.0.0/30";
            var request = new SubnetAllocationRequest { SubnetSize = 32 };

            _allocationServiceMock.Setup(x => x.AllocateNextSubnetAsync(addressSpaceId, parentCidr, request.SubnetSize, request.Tags, CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException("No available subnets"));

            // Act
            var result = await Controller.AllocateNextSubnet(addressSpaceId, parentCidr, request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("No available subnets", conflictResult.Value.ToString());
        }

        [Fact]
        public async Task AllocateNextSubnet_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var addressSpaceId = "space1";
            var parentCidr = "invalid-cidr";
            var request = new SubnetAllocationRequest { SubnetSize = 24 };

            _allocationServiceMock.Setup(x => x.AllocateNextSubnetAsync(addressSpaceId, parentCidr, request.SubnetSize, request.Tags, CancellationToken.None))
                .ThrowsAsync(new ArgumentException("Invalid CIDR"));

            // Act
            var result = await Controller.AllocateNextSubnet(addressSpaceId, parentCidr, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetUtilizationReport_ValidAddressSpace_ReturnsOkWithReport()
        {
            // Arrange
            var addressSpaceId = "space1";
            var ipv4Stats = new IpUtilizationStats
            {
                NetworkCidr = "0.0.0.0/0",
                UtilizationPercentage = 25.0,
                SubnetCount = 10,
                FragmentationIndex = 0.1
            };
            var ipv6Stats = new IpUtilizationStats
            {
                NetworkCidr = "::/0",
                UtilizationPercentage = 15.0,
                SubnetCount = 5,
                FragmentationIndex = 0.05
            };

            _allocationServiceMock.Setup(x => x.CalculateUtilizationAsync(addressSpaceId, "0.0.0.0/0", CancellationToken.None))
                .ReturnsAsync(ipv4Stats);
            _allocationServiceMock.Setup(x => x.CalculateUtilizationAsync(addressSpaceId, "::/0", CancellationToken.None))
                .ReturnsAsync(ipv6Stats);

            // Act
            var result = await Controller.GetUtilizationReport(addressSpaceId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var report = okResult.Value;
            Assert.NotNull(report);
        }

        [Fact]
        public async Task GetUtilizationReport_ServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var addressSpaceId = "space1";

            _allocationServiceMock.Setup(x => x.CalculateUtilizationAsync(addressSpaceId, "0.0.0.0/0", CancellationToken.None))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await Controller.GetUtilizationReport(addressSpaceId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }
    }
}