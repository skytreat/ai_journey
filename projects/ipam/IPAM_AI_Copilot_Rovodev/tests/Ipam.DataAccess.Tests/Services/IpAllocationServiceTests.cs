using Xunit;
using Moq;
using AutoMapper;
using Ipam.DataAccess.Services;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipam.DataAccess.Tests.TestHelpers;
using Ipam.ServiceContract.Models;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Unit tests for IpAllocationServiceImpl
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpAllocationServiceTests
    {
        private readonly Mock<IIpAllocationRepository> _ipNodeRepositoryMock;
        private readonly Mock<IpTreeService> _ipTreeServiceMock;
        private readonly Mock<ConcurrentIpTreeService> _concurrentIpTreeServiceMock;
        private readonly Mock<PerformanceMonitoringService> _performanceServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<IpAllocationServiceImpl>> _loggerMock;
        private readonly IpAllocationServiceImpl _service;

        public IpAllocationServiceTests()
        {
            _ipNodeRepositoryMock = new Mock<IIpAllocationRepository>();
            var tagInheritanceServiceMock = new Mock<TagInheritanceService>(new Mock<ITagRepository>().Object);
            _ipTreeServiceMock = new Mock<IpTreeService>(new Mock<IIpAllocationRepository>().Object, tagInheritanceServiceMock.Object);
            _concurrentIpTreeServiceMock = new Mock<ConcurrentIpTreeService>(new Mock<IIpAllocationRepository>().Object, tagInheritanceServiceMock.Object);
            _performanceServiceMock = new Mock<PerformanceMonitoringService>(new Mock<IMeterFactory>().Object, new Mock<ILogger<PerformanceMonitoringService>>().Object);
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<IpAllocationServiceImpl>>();

            _service = new IpAllocationServiceImpl(
                _ipNodeRepositoryMock.Object,
                _ipTreeServiceMock.Object,
                _concurrentIpTreeServiceMock.Object,
                _performanceServiceMock.Object,
                _mapperMock.Object,
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
                .ReturnsAsync(new List<IpAllocationEntity>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.FindAvailableSubnetsAsync("space1", parentCidr, subnetSize, count);

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

            var existingNodes = TestDataBuilders.CreateIpAllocationList(
                "10.0.1.0/24", 
                "10.0.2.0/24"
            );

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(existingNodes);

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.FindAvailableSubnetsAsync("space1", parentCidr, subnetSize, count);

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
                .ReturnsAsync(new List<IpAllocationEntity>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IpUtilizationStats>>>(),
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
            var subnets = TestDataBuilders.CreateIpAllocationList(
                "10.0.0.0/26",   // 64 addresses
                "10.0.0.64/26"   // 64 addresses
            );

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(subnets);

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IpUtilizationStats>>>(),
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
                .ReturnsAsync(TestDataBuilders.CreateIpAllocationList(
                    "10.0.2.0/24",
                    "10.0.3.0/24"
                ));

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
                .ReturnsAsync(TestDataBuilders.CreateIpAllocationList(
                    "10.0.1.0/25",  // Overlaps with proposed
                    "10.0.2.0/24"
                ));

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
            var expectedPrefix = "10.0.0.0/24";

            var expectedNode = new IpAllocationEntity
            {
                Id = "test-id",
                Prefix = expectedPrefix,
                Tags = tags
            };

            _ipNodeRepositoryMock.Setup(x => x.GetChildrenAsync("space1", null))
                .ReturnsAsync(new List<IpAllocationEntity>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            _ipTreeServiceMock.Setup(x => x.CreateIpAllocationAsync("space1", expectedPrefix, tags))
                .ReturnsAsync(expectedNode);

            // Act
            var result = await _service.AllocateNextSubnetAsync("space1", parentCidr, subnetSize, tags);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedPrefix, result.Prefix);
            Assert.Equal(tags, result.Tags);
            _ipTreeServiceMock.Verify(x => x.CreateIpAllocationAsync("space1", expectedPrefix, tags), Times.Once);
        }

        [Fact]
        public async Task AllocateNextSubnet_NoAvailableSubnets_ThrowsException()
        {
            // Arrange
            var parentCidr = "10.0.0.0/30"; // Very small network
            var subnetSize = 32; // Individual hosts

            // Fill up all available space
            var existingNodes = TestDataBuilders.CreateSequentialIpAllocations("10.0.0.0/32", 4);

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
                .ReturnsAsync(new List<IpAllocationEntity>());

            _performanceServiceMock.Setup(x => x.MeasureAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<string>>>>(),
                It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Func<Task<List<string>>>, Dictionary<string, object>>(
                    (name, func, tags) => func());

            // Act
            var result = await _service.FindAvailableSubnetsAsync("space1", parentCidr, subnetSize, requestedCount);

            // Assert
            Assert.Equal(requestedCount, result.Count);
            Assert.All(result, subnet => Assert.Contains($"/{subnetSize}", subnet));
        }
    }
}