using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess;

namespace Ipam.DataAccess.Api.Controllers
{
    /// <summary>
    /// Health check controller for Data Access API
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IDataAccessService _dataAccessService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IDataAccessService dataAccessService,
            ILogger<HealthController> logger)
        {
            _dataAccessService = dataAccessService;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check
        /// </summary>
        [HttpGet]
        public ActionResult<object> GetHealth()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "IPAM Data Access API",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });
        }

        /// <summary>
        /// Detailed health check including database connectivity
        /// </summary>
        [HttpGet("detailed")]
        public async Task<ActionResult<object>> GetDetailedHealth()
        {
            try
            {
                // Test database connectivity by attempting to get address spaces
                var addressSpaces = await _dataAccessService.GetAddressSpacesAsync();
                
                return Ok(new
                {
                    Status = "Healthy",
                    Service = "IPAM Data Access API",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Database = new
                    {
                        Status = "Connected",
                        AddressSpaceCount = addressSpaces.Count()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                
                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    Service = "IPAM Data Access API",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Database = new
                    {
                        Status = "Disconnected",
                        Error = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Liveness probe for Kubernetes
        /// </summary>
        [HttpGet("live")]
        public ActionResult GetLiveness()
        {
            return Ok("Data Access API is alive");
        }

        /// <summary>
        /// Readiness probe for Kubernetes
        /// </summary>
        [HttpGet("ready")]
        public async Task<ActionResult> GetReadiness()
        {
            try
            {
                // Test if the service can handle requests
                await _dataAccessService.GetAddressSpacesAsync();
                return Ok("Data Access API is ready");
            }
            catch
            {
                return StatusCode(503, "Data Access API is not ready");
            }
        }
    }
}