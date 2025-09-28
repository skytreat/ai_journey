using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Service for monitoring system performance and collecting metrics
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private readonly ActivitySource _activitySource;

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
        {
            _logger = logger;
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
            _activitySource = new ActivitySource("Ipam.DataAccess.Performance");
        }

        /// <summary>
        /// Measures the execution time of an operation
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operationName">Name of the operation being measured</param>
        /// <param name="operation">The operation to execute and measure</param>
        /// <param name="tags">Additional tags for the measurement</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> MeasureAsync<T>(
            string operationName, 
            Func<Task<T>> operation, 
            Dictionary<string, object> tags = null)
        {
            using var activity = _activitySource.StartActivity(operationName);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Add tags to activity
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity?.SetTag(tag.Key, tag.Value?.ToString());
                    }
                }

                var result = await operation();
                
                stopwatch.Stop();
                RecordMetric(operationName, stopwatch.ElapsedMilliseconds, true, tags);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordMetric(operationName, stopwatch.ElapsedMilliseconds, false, tags);
                
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Records a custom metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Metric value</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="tags">Additional tags</param>
        public void RecordMetric(
            string metricName, 
            double value, 
            bool success = true, 
            Dictionary<string, object> tags = null)
        {
            var metric = _metrics.AddOrUpdate(metricName, 
                new PerformanceMetric(metricName),
                (key, existing) => existing);

            metric.RecordValue(value, success);

            // Log the metric
            _logger.LogInformation(
                "Performance Metric: {MetricName} = {Value}ms, Success: {Success}, Tags: {@Tags}",
                metricName, value, success, tags);
        }

        /// <summary>
        /// Gets performance statistics for a specific metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <returns>Performance statistics or null if metric doesn't exist</returns>
        public PerformanceStatistics? GetStatistics(string metricName)
        {
            return _metrics.TryGetValue(metricName, out var metric) 
                ? metric.GetStatistics() 
                : null;
        }

        /// <summary>
        /// Gets all performance metrics
        /// </summary>
        /// <returns>Dictionary of all metrics and their statistics</returns>
        public Dictionary<string, PerformanceStatistics> GetAllStatistics()
        {
            var result = new Dictionary<string, PerformanceStatistics>();
            foreach (var kvp in _metrics)
            {
                result[kvp.Key] = kvp.Value.GetStatistics();
            }
            return result;
        }

        /// <summary>
        /// Clears all collected statistics
        /// </summary>
        public void ClearStatistics()
        {
            _metrics.Clear();
        }

        /// <summary>
        /// Monitors IP tree operation performance
        /// </summary>
        public async Task<T> MeasureIpTreeOperationAsync<T>(
            string operation,
            string addressSpaceId,
            Func<Task<T>> treeOperation)
        {
            var tags = new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["AddressSpaceId"] = addressSpaceId,
                ["Category"] = "IpTree"
            };

            return await MeasureAsync($"IpTree.{operation}", treeOperation, tags);
        }

        /// <summary>
        /// Monitors tag inheritance performance
        /// </summary>
        public async Task<T> MeasureTagInheritanceAsync<T>(
            string operation,
            int tagCount,
            Func<Task<T>> inheritanceOperation)
        {
            var tags = new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["TagCount"] = tagCount,
                ["Category"] = "TagInheritance"
            };

            return await MeasureAsync($"TagInheritance.{operation}", inheritanceOperation, tags);
        }

        public void Dispose()
        {
            _activitySource?.Dispose();
        }
    }

    /// <summary>
    /// Represents a performance metric with statistics
    /// </summary>
    public class PerformanceMetric
    {
        private readonly object _lock = new object();
        private readonly List<double> _values = new List<double>();
        private int _successCount;
        private int _failureCount;

        public string Name { get; }
        public DateTime CreatedAt { get; }

        public PerformanceMetric(string name)
        {
            Name = name;
            CreatedAt = DateTime.UtcNow;
        }

        public void RecordValue(double value, bool success)
        {
            lock (_lock)
            {
                _values.Add(value);
                if (success)
                    _successCount++;
                else
                    _failureCount++;
            }
        }

        public PerformanceStatistics GetStatistics()
        {
            lock (_lock)
            {
                if (_values.Count == 0)
                    return new PerformanceStatistics(Name, 0, 0, 0, 0, 0, 0, 100);

                var sortedValues = new List<double>(_values);
                sortedValues.Sort();

                var count = sortedValues.Count;
                var sum = sortedValues.Sum();
                var average = sum / count;
                var min = sortedValues[0];
                var max = sortedValues[count - 1];
                var p95 = sortedValues[(int)(count * 0.95)];
                var successRate = _successCount * 100.0 / (_successCount + _failureCount);

                return new PerformanceStatistics(Name, count, average, min, max, p95, sum, successRate);
            }
        }
    }

    /// <summary>
    /// Performance statistics for a metric
    /// </summary>
    public class PerformanceStatistics
    {
        public string MetricName { get; }
        public int Count { get; }
        public double Average { get; }
        public double Min { get; }
        public double Max { get; }
        public double P95 { get; }
        public double Total { get; }
        public double SuccessRate { get; }

        public PerformanceStatistics(
            string metricName, 
            int count, 
            double average, 
            double min, 
            double max, 
            double p95, 
            double total, 
            double successRate)
        {
            MetricName = metricName;
            Count = count;
            Average = average;
            Min = min;
            Max = max;
            P95 = p95;
            Total = total;
            SuccessRate = successRate;
        }
    }
}