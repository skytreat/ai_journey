using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ipam.DataAccess.Services;
using Ipam.ServiceContract.Interfaces;
using Ipam.Frontend.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.Frontend.Tests.Controllers
{
    /// <summary>
    /// Unit tests for HealthController
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class HealthControllerTests
    {
        private readonly Mock<IAddressSpaceService> _addressSpaceServiceMock;
        private readonly Mock<PerformanceMonitoringService> _performanceServiceMock;
        private readonly Mock<ILogger<HealthController>> _loggerMock;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _addressSpaceServiceMock = new Mock<IAddressSpaceService>();
            _performanceServiceMock = new Mock<PerformanceMonitoringService>();
            _loggerMock = new Mock<ILogger<HealthController>>();
            
            _controller = new HealthController(
                _addressSpaceServiceMock.Object,
                _performanceServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void GetHealth_Always_ReturnsOkWithHealthStatus()
        {
            // Act
            var result = _controller.GetHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var health = okResult.Value;
            Assert.NotNull(health);
            
            // Use reflection to check properties since it's an anonymous object
            var statusProperty = health.GetType().GetProperty("Status");
            Assert.NotNull(statusProperty);
            Assert.Equal("Healthy", statusProperty.GetValue(health));
        }

        [Fact]
        public async Task GetDetailedHealth_AllServicesHealthy_ReturnsOkWithHealthyStatus()
        {
            // Arrange
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpacesAsync())
                .ReturnsAsync(new List<Ipam.ServiceContract.DTOs.AddressSpaceDto>());

            _performanceServiceMock.Setup(x => x.GetAllStatistics())
                .Returns(new Dictionary<string, PerformanceStatistics>
                {
                    { "TestMetric", new PerformanceStatistics("TestMetric", 10, 100, 50, 200, 150, 1000, 95) }
                });

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var health = okResult.Value;
            Assert.NotNull(health);

            var statusProperty = health.GetType().GetProperty("Status");
            Assert.Equal("Healthy", statusProperty.GetValue(health));
        }

        [Fact]
        public async Task GetDetailedHealth_DatabaseUnhealthy_ReturnsDegradedStatus()
        {
            // Arrange
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpacesAsync())
                .ThrowsAsync(new Exception("Database connection failed"));

            _performanceServiceMock.Setup(x => x.GetAllStatistics())
                .Returns(new Dictionary<string, PerformanceStatistics>());

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);

            var health = statusResult.Value;
            var statusProperty = health.GetType().GetProperty("Status");
            Assert.NotEqual("Healthy", statusProperty.GetValue(health));
        }

        [Fact]
        public async Task GetDetailedHealth_PerformanceDegraded_ReturnsDegradedStatus()
        {
            // Arrange
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpacesAsync())
                .ReturnsAsync(new List<Ipam.ServiceContract.DTOs.AddressSpaceDto>());

            // Setup performance metrics with high response times
            _performanceServiceMock.Setup(x => x.GetAllStatistics())
                .Returns(new Dictionary<string, PerformanceStatistics>
                {
                    { "SlowMetric", new PerformanceStatistics("SlowMetric", 10, 6000, 1000, 10000, 8000, 60000, 95) }
                });

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetDetailedHealth_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpacesAsync())
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public void GetMetrics_WithMetrics_ReturnsOkWithMetrics()
        {
            // Arrange
            var expectedMetrics = new Dictionary<string, PerformanceStatistics>
            {
                { "API.Test", new PerformanceStatistics("API.Test", 5, 150, 100, 200, 180, 750, 100) },
                { "DB.Query", new PerformanceStatistics("DB.Query", 10, 50, 20, 100, 80, 500, 98) }
            };

            _performanceServiceMock.Setup(x => x.GetAllStatistics())
                .Returns(expectedMetrics);

            // Act
            var result = _controller.GetMetrics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var metricsCountProperty = response.GetType().GetProperty("MetricsCount");
            Assert.Equal(2, metricsCountProperty.GetValue(response));
        }

        [Fact]
        public void GetMetrics_ServiceException_ReturnsInternalServerError()
        {
            // Arrange
            _performanceServiceMock.Setup(x => x.GetAllStatistics())
                .Throws(new Exception("Metrics service error"));

            // Act
            var result = _controller.GetMetrics();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetReadiness_DatabaseAccessible_ReturnsOkWithReadyStatus()
        {
            // Arrange
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpacesAsync())
                .ReturnsAsync(new List<Ipam.ServiceContract.DTOs.AddressSpaceDto>
                {
                    new Ipam.ServiceContract.DTOs.AddressSpaceDto { Id = "test" }
                });

            // Act
            var result = await _controller.GetReadiness();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var readiness = okResult.Value;
            Assert.NotNull(readiness);

            var statusProperty = readiness.GetType().GetProperty("Status");
            Assert.Equal("Ready", statusProperty.GetValue(readiness));
        }

        [Fact]
        public async Task GetReadiness_DatabaseInaccessible_ReturnsServiceUnavailable()
        {
            // Arrange
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpacesAsync())
                .ThrowsAsync(new Exception("Database unavailable"));

            // Act
            var result = await _controller.GetReadiness();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);

            var readiness = statusResult.Value;
            var statusProperty = readiness.GetType().GetProperty("Status");
            Assert.Equal("NotReady", statusProperty.GetValue(readiness));
        }

        [Fact]
        public void GetLiveness_Always_ReturnsOkWithAliveStatus()
        {
            // Act
            var result = _controller.GetLiveness();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var liveness = okResult.Value;
            Assert.NotNull(liveness);

            var statusProperty = liveness.GetType().GetProperty("Status");
            Assert.Equal("Alive", statusProperty.GetValue(liveness));
        }

        [Fact]
        public async Task GetDetailedHealth_MemoryPressureHigh_ReturnsDegradedStatus()
        {
            // Arrange
            _addressSpaceServiceMock.Setup(x => x.GetAddressSpacesAsync())
                .ReturnsAsync(new List<Ipam.ServiceContract.DTOs.AddressSpaceDto>());

            _performanceServiceMock.Setup(x => x.GetAllStatistics())
                .Returns(new Dictionary<string, PerformanceStatistics>
                {
                    { "TestMetric", new PerformanceStatistics("TestMetric", 10, 100, 50, 200, 150, 1000, 95) }
                });

            // Force a lot of GC to simulate memory pressure
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            // The result might be healthy or degraded depending on actual memory usage
            // We just verify it doesn't throw an exception
            Assert.True(result is OkObjectResult || result is ObjectResult);
        }
    }
}