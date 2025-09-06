using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Models;
using Ipam.DataAccess;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for managing address spaces
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AddressSpacesController : ControllerBase
    {
        private readonly IDataAccessService _dataAccessService;

        public AddressSpacesController(IDataAccessService dataAccessService)
        {
            _dataAccessService = dataAccessService;
        }

        /// <summary>
        /// Creates a new address space
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> CreateAddressSpace([FromBody] AddressSpaceCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var addressSpace = new AddressSpace
            {
                Name = model.Name,
                Description = model.Description,
                CreatedOn = DateTime.UtcNow
            };

            var result = await _dataAccessService.CreateAddressSpaceAsync(addressSpace);
            return CreatedAtAction(nameof(GetAddressSpace), new { addressSpaceId = result.Id }, result);
        }

        /// <summary>
        /// Gets an address space by ID
        /// </summary>
        [HttpGet("{addressSpaceId}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "addressSpaceId" })]
        public async Task<IActionResult> GetAddressSpace(string addressSpaceId)
        {
            var addressSpace = await _dataAccessService.GetAddressSpaceAsync(addressSpaceId);
            if (addressSpace == null)
            {
                return NotFound();
            }
            return Ok(addressSpace);
        }

        /// <summary>
        /// Gets all address spaces with optional filtering
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "nameFilter", "createdAfter" })]
        public async Task<IActionResult> GetAddressSpaces([FromQuery] AddressSpaceQueryModel query)
        {
            var addressSpaces = await _dataAccessService.GetAddressSpacesAsync(
                query.NameFilter,
                query.CreatedAfter);
                
            return Ok(addressSpaces);
        }

        /// <summary>
        /// Updates an existing address space
        /// </summary>
        [HttpPut("{addressSpaceId}")]
        public async Task<IActionResult> UpdateAddressSpace(string addressSpaceId, [FromBody] AddressSpace addressSpace)
        {
            if (addressSpace == null || addressSpace.Id != addressSpaceId)
            {
                return BadRequest("Address space ID mismatch.");
            }

            var updatedAddressSpace = await _dataAccessService.UpdateAddressSpaceAsync(addressSpace);
            return Ok(updatedAddressSpace);
        }

        /// <summary>
        /// Deletes an address space
        /// </summary>
        [HttpDelete("{addressSpaceId}")]
        public async Task<IActionResult> DeleteAddressSpace(string addressSpaceId)
        {
            await _dataAccessService.DeleteAddressSpaceAsync(addressSpaceId);
            return NoContent();
        }
    }
}