using System.Collections.Generic;
using Ipam.DataAccess.Services;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for performance monitoring service
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// Records a metric with timing information
        /// </summary>
        void RecordMetric(string metricName, double value, bool success = true, Dictionary<string, object>? tags = null);

        /// <summary>
        /// Gets all performance statistics
        /// </summary>
        Dictionary<string, PerformanceStatistics> GetAllStatistics();

        /// <summary>
        /// Gets statistics for a specific metric
        /// </summary>
        PerformanceStatistics? GetStatistics(string metricName);

        /// <summary>
        /// Clears all collected statistics
        /// </summary>
        void ClearStatistics();
    }
}