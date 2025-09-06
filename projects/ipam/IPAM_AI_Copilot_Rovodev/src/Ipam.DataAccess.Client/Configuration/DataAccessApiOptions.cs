namespace Ipam.DataAccess.Client.Configuration
{
    /// <summary>
    /// Configuration options for Data Access API client
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class DataAccessApiOptions
    {
        public const string SectionName = "DataAccessApi";

        /// <summary>
        /// Base URL of the Data Access API
        /// </summary>
        public string BaseUrl { get; set; } = "https://localhost:5002";

        /// <summary>
        /// API key for authentication (JWT token)
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of retry attempts for failed requests
        /// </summary>
        public int RetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;
    }
}