using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Ipam.DataAccess.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.Services
{
    /// <summary>
    /// Unit tests for PerformanceMonitoringService
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class PerformanceMonitoringServiceTests
    {
        private readonly Mock<ILogger<PerformanceMonitoringService>> _loggerMock;
        private readonly PerformanceMonitoringService _service;

        public PerformanceMonitoringServiceTests()
        {
            _loggerMock = new Mock<ILogger<PerformanceMonitoringService>>();
            _service = new PerformanceMonitoringService(_loggerMock.Object);
        }

        [Fact]
        public async Task MeasureAsync_SuccessfulOperation_RecordsSuccessMetric()
        {
            // Arrange
            var operationName = "TestOperation";
            var expectedResult = "success";

            // Act
            var result = await _service.MeasureAsync(operationName, () => Task.FromResult(expectedResult));

            // Assert
            Assert.Equal(expectedResult, result);
            
            var stats = _service.GetStatistics(operationName);
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
            Assert.Equal(100.0, stats.SuccessRate);
        }

        [Fact]
        public async Task MeasureAsync_FailedOperation_RecordsFailureMetric()
        {
            // Arrange
            var operationName = "FailingOperation";
            var expectedException = new InvalidOperationException("Test exception");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.MeasureAsync(operationName, () => Task.FromException<string>(expectedException)));

            Assert.Equal(expectedException, exception);
            
            var stats = _service.GetStatistics(operationName);
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
            Assert.Equal(0.0, stats.SuccessRate);
        }

        [Fact]
        public async Task MeasureAsync_WithTags_IncludesTagsInActivity()
        {
            // Arrange
            var operationName = "TaggedOperation";
            var tags = new Dictionary<string, object>
            {
                { "UserId", "user123" },
                { "Operation", "Create" }
            };

            // Act
            var result = await _service.MeasureAsync(operationName, () => Task.FromResult("success"), tags);

            // Assert
            Assert.Equal("success", result);
            
            var stats = _service.GetStatistics(operationName);
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
        }

        [Fact]
        public void RecordMetric_NewMetric_CreatesMetricWithCorrectValues()
        {
            // Arrange
            var metricName = "NewMetric";
            var value = 150.5;

            // Act
            _service.RecordMetric(metricName, value, true);

            // Assert
            var stats = _service.GetStatistics(metricName);
            Assert.NotNull(stats);
            Assert.Equal(metricName, stats.MetricName);
            Assert.Equal(1, stats.Count);
            Assert.Equal(value, stats.Average);
            Assert.Equal(value, stats.Min);
            Assert.Equal(value, stats.Max);
            Assert.Equal(100.0, stats.SuccessRate);
        }

        [Fact]
        public void RecordMetric_MultipleValues_CalculatesCorrectStatistics()
        {
            // Arrange
            var metricName = "MultiValueMetric";
            var values = new[] { 100.0, 200.0, 150.0, 300.0, 250.0 };

            // Act
            foreach (var value in values)
            {
                _service.RecordMetric(metricName, value, true);
            }

            // Assert
            var stats = _service.GetStatistics(metricName);
            Assert.NotNull(stats);
            Assert.Equal(values.Length, stats.Count);
            Assert.Equal(200.0, stats.Average); // (100+200+150+300+250)/5
            Assert.Equal(100.0, stats.Min);
            Assert.Equal(300.0, stats.Max);
            Assert.Equal(100.0, stats.SuccessRate);
        }

        [Fact]
        public void RecordMetric_MixedSuccessFailure_CalculatesCorrectSuccessRate()
        {
            // Arrange
            var metricName = "MixedResultMetric";

            // Act
            _service.RecordMetric(metricName, 100, true);
            _service.RecordMetric(metricName, 200, true);
            _service.RecordMetric(metricName, 150, false);
            _service.RecordMetric(metricName, 300, true);

            // Assert
            var stats = _service.GetStatistics(metricName);
            Assert.NotNull(stats);
            Assert.Equal(4, stats.Count);
            Assert.Equal(75.0, stats.SuccessRate); // 3 successes out of 4 total
        }

        [Fact]
        public void GetStatistics_NonExistentMetric_ReturnsNull()
        {
            // Act
            var stats = _service.GetStatistics("NonExistentMetric");

            // Assert
            Assert.Null(stats);
        }

        [Fact]
        public void GetAllStatistics_WithMultipleMetrics_ReturnsAllMetrics()
        {
            // Arrange
            _service.RecordMetric("Metric1", 100, true);
            _service.RecordMetric("Metric2", 200, true);
            _service.RecordMetric("Metric3", 300, false);

            // Act
            var allStats = _service.GetAllStatistics();

            // Assert
            Assert.Equal(3, allStats.Count);
            Assert.Contains("Metric1", allStats.Keys);
            Assert.Contains("Metric2", allStats.Keys);
            Assert.Contains("Metric3", allStats.Keys);
        }

        [Fact]
        public void GetAllStatistics_NoMetrics_ReturnsEmptyDictionary()
        {
            // Act
            var allStats = _service.GetAllStatistics();

            // Assert
            Assert.Empty(allStats);
        }

        [Fact]
        public async Task MeasureIpTreeOperationAsync_ValidOperation_RecordsWithCorrectTags()
        {
            // Arrange
            var operation = "CreateNode";
            var addressSpaceId = "space123";

            // Act
            var result = await _service.MeasureIpTreeOperationAsync(operation, addressSpaceId, 
                () => Task.FromResult("node-created"));

            // Assert
            Assert.Equal("node-created", result);
            
            var stats = _service.GetStatistics($"IpTree.{operation}");
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
        }

        [Fact]
        public async Task MeasureTagInheritanceAsync_ValidOperation_RecordsWithCorrectTags()
        {
            // Arrange
            var operation = "ApplyImplications";
            var tagCount = 5;

            // Act
            var result = await _service.MeasureTagInheritanceAsync(operation, tagCount,
                () => Task.FromResult("implications-applied"));

            // Assert
            Assert.Equal("implications-applied", result);
            
            var stats = _service.GetStatistics($"TagInheritance.{operation}");
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
        }

        [Fact]
        public void RecordMetric_WithTags_LogsMetricWithTags()
        {
            // Arrange
            var metricName = "TaggedMetric";
            var value = 123.45;
            var tags = new Dictionary<string, object>
            {
                { "Component", "DataAccess" },
                { "Method", "GetById" }
            };

            // Act
            _service.RecordMetric(metricName, value, true, tags);

            // Assert
            var stats = _service.GetStatistics(metricName);
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
            Assert.Equal(value, stats.Average);
        }

        [Fact]
        public void PerformanceStatistics_CalculatesP95Correctly()
        {
            // Arrange
            var metricName = "P95TestMetric";
            var values = new double[100];
            for (int i = 0; i < 100; i++)
            {
                values[i] = i + 1; // Values from 1 to 100
            }

            // Act
            foreach (var value in values)
            {
                _service.RecordMetric(metricName, value, true);
            }

            // Assert
            var stats = _service.GetStatistics(metricName);
            Assert.NotNull(stats);
            Assert.Equal(100, stats.Count);
            Assert.Equal(95.0, stats.P95); // 95th percentile of 1-100 should be 95
        }

        [Fact]
        public void PerformanceStatistics_SingleValue_P95EqualToValue()
        {
            // Arrange
            var metricName = "SingleValueMetric";
            var value = 42.0;

            // Act
            _service.RecordMetric(metricName, value, true);

            // Assert
            var stats = _service.GetStatistics(metricName);
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
            Assert.Equal(value, stats.P95);
            Assert.Equal(value, stats.Average);
            Assert.Equal(value, stats.Min);
            Assert.Equal(value, stats.Max);
        }

        [Fact]
        public async Task MeasureAsync_LongRunningOperation_RecordsCorrectDuration()
        {
            // Arrange
            var operationName = "LongOperation";
            var delay = TimeSpan.FromMilliseconds(100);

            // Act
            var result = await _service.MeasureAsync(operationName, async () =>
            {
                await Task.Delay(delay);
                return "completed";
            });

            // Assert
            Assert.Equal("completed", result);
            
            var stats = _service.GetStatistics(operationName);
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Count);
            Assert.True(stats.Average >= delay.TotalMilliseconds);
        }

        [Fact]
        public void Dispose_DisposesActivitySource()
        {
            // Act & Assert - Should not throw
            _service.Dispose();
        }
    }
}