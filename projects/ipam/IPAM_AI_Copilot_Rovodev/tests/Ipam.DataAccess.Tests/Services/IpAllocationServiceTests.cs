using Xunit;
using Moq;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Unit tests for IpAllocationService
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpAllocationServiceTests
    {
        private readonly Mock<IIpNodeRepository> _ipNodeRepositoryMock;
        private readonly Mock<IpTreeService> _ipTreeServiceMock;
        private readonly Mock<PerformanceMonitoringService> _performanceServiceMock;
        private readonly Mock<ILogger<IpAllocationService>> _loggerMock;
        private readonly IpAllocationService _service;

        public IpAllocationServiceTests()
        {
            _ipNodeRepositoryMock = new Mock<IIpNodeRepository>();
            _ipTreeServiceMock = new Mock<IpTreeService>();
            _performanceServiceMock = new Mock<PerformanceMonitoringService>();
            _loggerMock = new Mock<ILogger<IpAllocationService>>();

            _service = new IpAllocationService(
                _ipNodeRepositoryMock.Object,
                _ipTreeServiceMock.Object,
                _performanceServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task FindAvailableSubnets_NoExistingNodes_ReturnsRequestedCount()
        {
            // Arrange
            var parentCidr = "10.0.0.0/8";
            var subnetSize = 24;
            var count = 5;

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(new List<IpNode>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.FindAvailableSubnets("space1", parentCidr, subnetSize, count);

            // Assert
            Assert.Equal(count, result.Count);
            Assert.All(result, subnet => Assert.Contains("/24", subnet));
        }

        [Fact]
        public async Task FindAvailableSubnets_WithExistingNodes_ExcludesConflicts()
        {
            // Arrange
            var parentCidr = "10.0.0.0/16";
            var subnetSize = 24;
            var count = 3;

            var existingNodes = new List<IpNode>
            {
                new IpNode { Prefix = "10.0.1.0/24" },
                new IpNode { Prefix = "10.0.2.0/24" }
            };

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(existingNodes);

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.FindAvailableSubnets("space1", parentCidr, subnetSize, count);

            // Assert
            Assert.Equal(count, result.Count);
            Assert.DoesNotContain("10.0.1.0/24", result);
            Assert.DoesNotContain("10.0.2.0/24", result);
        }

        [Fact]
        public async Task CalculateUtilization_EmptyNetwork_ReturnsZeroUtilization()
        {
            // Arrange
            var networkCidr = "10.0.0.0/24";

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(new List<IpNode>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IpUtilizationStats>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<IpUtilizationStats>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.CalculateUtilizationAsync("space1", networkCidr);

            // Assert
            Assert.Equal(networkCidr, result.NetworkCidr);
            Assert.Equal(0, result.UtilizationPercentage);
            Assert.Equal(0, result.SubnetCount);
            Assert.True(result.TotalAddresses > 0);
        }

        [Fact]
        public async Task CalculateUtilization_WithSubnets_CalculatesCorrectUtilization()
        {
            // Arrange
            var networkCidr = "10.0.0.0/24";
            var subnets = new List<IpNode>
            {
                new IpNode { Prefix = "10.0.0.0/26" },   // 64 addresses
                new IpNode { Prefix = "10.0.0.64/26" }   // 64 addresses
            };

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(subnets);

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IpUtilizationStats>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<IpUtilizationStats>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.CalculateUtilizationAsync("space1", networkCidr);

            // Assert
            Assert.Equal(networkCidr, result.NetworkCidr);
            Assert.Equal(2, result.SubnetCount);
            Assert.True(result.UtilizationPercentage > 0);
            Assert.True(result.UtilizationPercentage <= 100);
        }

        [Fact]
        public async Task ValidateSubnetAllocation_NoConflicts_ReturnsValid()
        {
            // Arrange
            var proposedCidr = "10.0.1.0/24";

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(new List<IpNode>
                {
                    new IpNode { Prefix = "10.0.2.0/24" },
                    new IpNode { Prefix = "10.0.3.0/24" }
                });

            // Act
            var result = await _service.ValidateSubnetAllocationAsync("space1", proposedCidr);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(proposedCidr, result.ProposedCidr);
            Assert.Empty(result.ConflictingSubnets);
        }

        [Fact]
        public async Task ValidateSubnetAllocation_WithConflicts_ReturnsInvalid()
        {
            // Arrange
            var proposedCidr = "10.0.1.0/24";

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(new List<IpNode>
                {
                    new IpNode { Prefix = "10.0.1.0/25" },  // Overlaps with proposed
                    new IpNode { Prefix = "10.0.2.0/24" }
                });

            // Act
            var result = await _service.ValidateSubnetAllocationAsync("space1", proposedCidr);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("10.0.1.0/25", result.ConflictingSubnets);
            Assert.DoesNotContain("10.0.2.0/24", result.ConflictingSubnets);
        }

        [Fact]
        public async Task AllocateNextSubnet_AvailableSubnet_ReturnsAllocatedNode()
        {
            // Arrange
            var parentCidr = "10.0.0.0/16";
            var subnetSize = 24;
            var tags = new Dictionary<string, string> { { "Environment", "Test" } };

            var expectedNode = new IpNode
            {
                Id = "test-id",
                Prefix = "10.0.0.0/24",
                Tags = tags
            };

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(new List<IpNode>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            _ipTreeServiceMock.Setup(x => x.CreateIpNodeAsync("space1", It.IsAny<string>(), tags))
                .ReturnsAsync(expectedNode);

            // Act
            var result = await _service.AllocateNextSubnetAsync("space1", parentCidr, subnetSize, tags);

            // Assert
            Assert.Equal(expectedNode, result);
            _ipTreeServiceMock.Verify(x => x.CreateIpNodeAsync("space1", It.IsAny<string>(), tags), Times.Once);
        }

        [Fact]
        public async Task AllocateNextSubnet_NoAvailableSubnets_ThrowsException()
        {
            // Arrange
            var parentCidr = "10.0.0.0/30"; // Very small network
            var subnetSize = 32; // Individual hosts

            // Fill up all available space
            var existingNodes = new List<IpNode>
            {
                new IpNode { Prefix = "10.0.0.0/32" },
                new IpNode { Prefix = "10.0.0.1/32" },
                new IpNode { Prefix = "10.0.0.2/32" },
                new IpNode { Prefix = "10.0.0.3/32" }
            };

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(existingNodes);

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.AllocateNextSubnetAsync("space1", parentCidr, subnetSize));
        }

        [Theory]
        [InlineData("10.0.0.0/8", 16, 1)]
        [InlineData("192.168.0.0/16", 24, 5)]
        [InlineData("172.16.0.0/12", 20, 10)]
        public async Task FindAvailableSubnets_VariousNetworkSizes_ReturnsCorrectCount(
            string parentCidr, int subnetSize, int requestedCount)
        {
            // Arrange
            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(new List<IpNode>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.FindAvailableSubnets("space1", parentCidr, subnetSize, requestedCount);

            // Assert
            Assert.Equal(requestedCount, result.Count);
            Assert.All(result, subnet => Assert.Contains($"/{subnetSize}", subnet));
        }
    }
}