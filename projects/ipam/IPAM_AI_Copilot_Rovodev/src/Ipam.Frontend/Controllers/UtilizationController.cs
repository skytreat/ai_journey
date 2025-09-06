using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for IP utilization analytics and reporting
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/addressspaces/{addressSpaceId}/utilization")]
    public class UtilizationController : ControllerBase
    {
        private readonly IpAllocationService _allocationService;
        private readonly PerformanceMonitoringService _performanceService;
        private readonly AuditService _auditService;

        public UtilizationController(
            IpAllocationService allocationService,
            PerformanceMonitoringService performanceService,
            AuditService auditService)
        {
            _allocationService = allocationService;
            _performanceService = performanceService;
            _auditService = auditService;
        }

        /// <summary>
        /// Gets utilization statistics for a specific network
        /// </summary>
        [HttpGet("{networkCidr}")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "addressSpaceId", "networkCidr" })]
        public async Task<IActionResult> GetUtilization(string addressSpaceId, string networkCidr)
        {
            try
            {
                var stats = await _allocationService.CalculateUtilizationAsync(addressSpaceId, networkCidr);
                
                await _auditService.LogAuditEventAsync(
                    "UtilizationQuery", 
                    "Network", 
                    networkCidr, 
                    User.Identity?.Name ?? "Anonymous",
                    metadata: new Dictionary<string, object> { ["AddressSpaceId"] = addressSpaceId });

                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Finds available subnets within a parent network
        /// </summary>
        [HttpGet("{parentCidr}/available")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "addressSpaceId", "parentCidr", "subnetSize", "count" })]
        public async Task<IActionResult> FindAvailableSubnets(
            string addressSpaceId, 
            string parentCidr,
            [FromQuery] int subnetSize,
            [FromQuery] int count = 10)
        {
            if (subnetSize <= 0 || subnetSize > 128)
                return BadRequest("Invalid subnet size");

            if (count <= 0 || count > 100)
                return BadRequest("Count must be between 1 and 100");

            try
            {
                var availableSubnets = await _allocationService.FindAvailableSubnetsAsync(
                    addressSpaceId, parentCidr, subnetSize, count);

                return Ok(new
                {
                    ParentNetwork = parentCidr,
                    SubnetSize = subnetSize,
                    RequestedCount = count,
                    AvailableCount = availableSubnets.Count,
                    AvailableSubnets = availableSubnets
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Validates a proposed subnet allocation
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateSubnetAllocation(
            string addressSpaceId,
            [FromBody] SubnetValidationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _allocationService.ValidateSubnetAllocationAsync(
                    addressSpaceId, request.ProposedCidr);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Allocates the next available subnet automatically
        /// </summary>
        [HttpPost("{parentCidr}/allocate")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin,AddressSpaceOperator")]
        public async Task<IActionResult> AllocateNextSubnet(
            string addressSpaceId,
            string parentCidr,
            [FromBody] SubnetAllocationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var allocatedNode = await _allocationService.AllocateNextSubnetAsync(
                    addressSpaceId, parentCidr, request.SubnetSize, request.Tags);

                await _auditService.LogAuditEventAsync(
                    "AutoAllocateSubnet",
                    "IpNode",
                    allocatedNode.Id,
                    User.Identity?.Name ?? "System",
                    new Dictionary<string, object>
                    {
                        ["AllocatedCidr"] = allocatedNode.Prefix,
                        ["ParentCidr"] = parentCidr,
                        ["SubnetSize"] = request.SubnetSize
                    },
                    new Dictionary<string, object> { ["AddressSpaceId"] = addressSpaceId });

                return CreatedAtAction(
                    "GetById",
                    "IpNode",
                    new { addressSpaceId = addressSpaceId, ipId = allocatedNode.Id },
                    allocatedNode);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets comprehensive utilization report for the entire address space
        /// </summary>
        [HttpGet("report")]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "addressSpaceId" })]
        public async Task<IActionResult> GetUtilizationReport(string addressSpaceId)
        {
            try
            {
                // Get utilization for both IPv4 and IPv6 root networks
                var ipv4Stats = await _allocationService.CalculateUtilizationAsync(addressSpaceId, "0.0.0.0/0");
                var ipv6Stats = await _allocationService.CalculateUtilizationAsync(addressSpaceId, "::/0");

                var report = new
                {
                    AddressSpaceId = addressSpaceId,
                    GeneratedAt = DateTime.UtcNow,
                    IPv4Utilization = ipv4Stats,
                    IPv6Utilization = ipv6Stats,
                    OverallUtilization = new
                    {
                        TotalSubnets = ipv4Stats.SubnetCount + ipv6Stats.SubnetCount,
                        AverageUtilization = (ipv4Stats.UtilizationPercentage + ipv6Stats.UtilizationPercentage) / 2,
                        FragmentationScore = (ipv4Stats.FragmentationIndex + ipv6Stats.FragmentationIndex) / 2
                    }
                };

                await _auditService.LogAuditEventAsync(
                    "UtilizationReport",
                    "AddressSpace",
                    addressSpaceId,
                    User.Identity?.Name ?? "Anonymous");

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate utilization report", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for subnet validation
    /// </summary>
    public class SubnetValidationRequest
    {
        public string ProposedCidr { get; set; }
    }

    /// <summary>
    /// Request model for subnet allocation
    /// </summary>
    public class SubnetAllocationRequest
    {
        public int SubnetSize { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }
}