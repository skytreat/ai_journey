using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Services;
using Ipam.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ipam.ServiceContract.Interfaces;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for system health monitoring and diagnostics
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly IAddressSpaceService _addressSpaceService;
        private readonly PerformanceMonitoringService _performanceService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IAddressSpaceService addressSpaceService,
            PerformanceMonitoringService performanceService,
            ILogger<HealthController> logger)
        {
            _addressSpaceService = addressSpaceService;
            _performanceService = performanceService;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check endpoint
        /// </summary>
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString(),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }

        /// <summary>
        /// Detailed health check with dependency validation
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailedHealth()
        {
            var healthChecks = new Dictionary<string, object>();
            var overallStatus = "Healthy";

            try
            {
                // Check database connectivity
                var dbHealth = await CheckDatabaseHealth();
                healthChecks["Database"] = dbHealth;
                if (((dynamic)dbHealth).Status != "Healthy") overallStatus = "Degraded";

                // Check performance metrics
                var performanceHealth = CheckPerformanceHealth();
                healthChecks["Performance"] = performanceHealth;
                if (((dynamic)performanceHealth).Status != "Healthy" && overallStatus == "Healthy") 
                    overallStatus = "Degraded";

                // Check memory usage
                var memoryHealth = CheckMemoryHealth();
                healthChecks["Memory"] = memoryHealth;
                if (((dynamic)memoryHealth).Status != "Healthy" && overallStatus == "Healthy") 
                    overallStatus = "Degraded";

                // System information
                healthChecks["System"] = new
                {
                    Status = "Healthy",
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    OSVersion = Environment.OSVersion.ToString(),
                    WorkingSet = GC.GetTotalMemory(false),
                    Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
                };

                var response = new
                {
                    Status = overallStatus,
                    Timestamp = DateTime.UtcNow,
                    Checks = healthChecks
                };

                return overallStatus == "Healthy" ? Ok(response) : StatusCode(503, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets performance metrics and statistics
        /// </summary>
        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            try
            {
                var allStats = _performanceService.GetAllStatistics();
                
                return Ok(new
                {
                    Timestamp = DateTime.UtcNow,
                    MetricsCount = allStats.Count,
                    Metrics = allStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve metrics");
                return StatusCode(500, new { error = "Failed to retrieve metrics" });
            }
        }

        /// <summary>
        /// Readiness probe for Kubernetes
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> GetReadiness()
        {
            try
            {
                // Quick database connectivity check
                var addressSpaces = await _addressSpaceService.GetAddressSpacesAsync();
                
                return Ok(new
                {
                    Status = "Ready",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Readiness check failed");
                return StatusCode(503, new
                {
                    Status = "NotReady",
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Liveness probe for Kubernetes
        /// </summary>
        [HttpGet("live")]
        public IActionResult GetLiveness()
        {
            // Simple liveness check - if we can respond, we're alive
            return Ok(new
            {
                Status = "Alive",
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task<object> CheckDatabaseHealth()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var addressSpaces = await _addressSpaceService.GetAddressSpacesAsync();
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new
                {
                    Status = "Healthy",
                    ResponseTimeMs = responseTime,
                    AddressSpaceCount = addressSpaces?.Count() ?? 0
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Status = "Unhealthy",
                    Error = ex.Message
                };
            }
        }

        private object CheckPerformanceHealth()
        {
            try
            {
                var stats = _performanceService.GetAllStatistics();
                var avgResponseTime = stats.Values.Any() ? stats.Values.Average(s => s.Average) : 0;
                var avgSuccessRate = stats.Values.Any() ? stats.Values.Average(s => s.SuccessRate) : 100;

                var status = "Healthy";
                if (avgResponseTime > 5000) status = "Degraded"; // > 5 seconds
                if (avgSuccessRate < 95) status = "Unhealthy"; // < 95% success rate

                return new
                {
                    Status = status,
                    AverageResponseTimeMs = avgResponseTime,
                    AverageSuccessRate = avgSuccessRate,
                    MetricsCount = stats.Count
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Status = "Unknown",
                    Error = ex.Message
                };
            }
        }

        private object CheckMemoryHealth()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                var workingSet = Environment.WorkingSet;
                var memoryPressure = totalMemory / (double)workingSet;

                var status = "Healthy";
                if (memoryPressure > 0.8) status = "Degraded"; // > 80% memory usage
                if (memoryPressure > 0.95) status = "Unhealthy"; // > 95% memory usage

                return new
                {
                    Status = status,
                    TotalMemoryBytes = totalMemory,
                    WorkingSetBytes = workingSet,
                    MemoryPressure = memoryPressure,
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Status = "Unknown",
                    Error = ex.Message
                };
            }
        }
    }
}