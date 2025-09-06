using System;

namespace Ipam.DataAccess.Configuration
{
    /// <summary>
    /// Configuration options for data access services
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class DataAccessOptions
    {
        public string ConnectionString { get; set; }
        public bool EnableCaching { get; set; }
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}
