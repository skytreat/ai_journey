using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ipam.ServiceContract.DTOs;
using Ipam.ServiceContract.Interfaces;
using Ipam.Frontend.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for managing IP nodes within address spaces
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/addressspaces/{addressSpaceId}/ipnodes")]
    public class IpAllocationController : ControllerBase
    {
        private readonly IIpAllocationService _ipAllocationService;

        public IpAllocationController(IIpAllocationService ipAllocationService)
        {
            _ipAllocationService = ipAllocationService;
        }

        /// <summary>
        /// Gets a specific IP node by ID within an address space
        /// </summary>
        [HttpGet("{ipId}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "addressSpaceId", "ipId" })]
        public async Task<IActionResult> GetById(string addressSpaceId, string ipId)
        {
            var ipAllocation = await _ipAllocationService.GetIpAllocationByIdAsync(addressSpaceId, ipId);
            if (ipAllocation == null)
                return NotFound();

            return Ok(ipAllocation);
        }

        /// <summary>
        /// Gets all IP nodes within an address space with optional filtering
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "addressSpaceId", "cidr", "tags" })]
        public async Task<IActionResult> GetAll(string addressSpaceId, [FromQuery] string cidr = null, [FromQuery] Dictionary<string, string> tags = null)
        {
            var ipAllocations = await _ipAllocationService.GetIpAllocationsAsync(addressSpaceId, CancellationToken.None);
            return Ok(ipAllocations);
        }

        /// <summary>
        /// Creates a new IP node within an address space
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin,AddressSpaceOperator")]
        public async Task<IActionResult> Create(string addressSpaceId, [FromBody] IpNodeCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ensure the model's address space ID matches the route parameter
            if (model.AddressSpaceId != addressSpaceId)
                return BadRequest("Address space ID mismatch between route and model.");

            var ipAllocation = new IpAllocation
            {
                Id = Guid.NewGuid().ToString(),
                AddressSpaceId = addressSpaceId,
                Prefix = model.Prefix,
                Tags = model.Tags ?? new Dictionary<string, string>(),
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            var result = await _ipAllocationService.CreateIpAllocationAsync(ipAllocation);
            return CreatedAtAction(nameof(GetById), 
                new { addressSpaceId = addressSpaceId, ipId = result.Id }, 
                result);
        }

        /// <summary>
        /// Updates an existing IP node within an address space
        /// </summary>
        [HttpPut("{ipId}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin,AddressSpaceOperator")]
        public async Task<IActionResult> Update(string addressSpaceId, string ipId, [FromBody] IpNodeUpdateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get existing IP allocation
            var existingIpAllocation = await _ipAllocationService.GetIpAllocationByIdAsync(addressSpaceId, ipId);
            if (existingIpAllocation == null)
                return NotFound();

            // Update properties
            existingIpAllocation.Prefix = model.Prefix;
            existingIpAllocation.Tags = model.Tags ?? new Dictionary<string, string>();
            existingIpAllocation.ModifiedOn = DateTime.UtcNow;

            var updatedIpAllocation = await _ipAllocationService.UpdateIpAllocationAsync(existingIpAllocation);
            return Ok(updatedIpAllocation);
        }

        /// <summary>
        /// Deletes an IP node from an address space
        /// </summary>
        [HttpDelete("{ipId}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin,AddressSpaceOperator")]
        public async Task<IActionResult> Delete(string addressSpaceId, string ipId)
        {
            var existingIpAllocation = await _ipAllocationService.GetIpAllocationByIdAsync(addressSpaceId, ipId);
            if (existingIpAllocation == null)
                return NotFound();

            await _ipAllocationService.DeleteIpAllocationAsync(addressSpaceId, ipId, CancellationToken.None);
            return NoContent();
        }

        /// <summary>
        /// Gets IP nodes by CIDR prefix within an address space
        /// </summary>
        [HttpGet("byPrefix")]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "addressSpaceId", "prefix" })]
        public async Task<IActionResult> GetByPrefix(string addressSpaceId, [FromQuery] [Required] string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return BadRequest("Prefix parameter is required.");

            try
            {
                var prefixObj = new ServiceContract.Models.Prefix(prefix);
                var ipAllocations = await _ipAllocationService.GetIpAllocationsByPrefixAsync(addressSpaceId, prefixObj, CancellationToken.None);
                return Ok(ipAllocations);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Invalid prefix format: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets IP nodes by tags within an address space
        /// </summary>
        [HttpGet("byTags")]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "addressSpaceId", "tags" })]
        public async Task<IActionResult> GetByTags(string addressSpaceId, [FromQuery] Dictionary<string, string> tags)
        {
            if (tags == null || !tags.Any())
                return BadRequest("At least one tag must be specified.");

            try
            {
                var ipAllocations = await _ipAllocationService.GetIpAllocationsByTagsAsync(addressSpaceId, tags, CancellationToken.None);
                return Ok(ipAllocations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving IP allocations by tags: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets child IP nodes of a specific IP node within an address space
        /// </summary>
        [HttpGet("{ipId}/children")]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "addressSpaceId", "ipId" })]
        public async Task<IActionResult> GetChildren(string addressSpaceId, string ipId)
        {
            // First verify the parent IP exists
            var parentIp = await _ipAllocationService.GetIpAllocationByIdAsync(addressSpaceId, ipId);
            if (parentIp == null)
                return NotFound("Parent IP node not found.");

            // Get all IPs in the address space and filter for children
            var allIps = await _ipAllocationService.GetIpAllocationsAsync(addressSpaceId, CancellationToken.None);
            var children = allIps.Where(ip => ip.ParentId == ipId);
            
            return Ok(children);
        }
    }
}