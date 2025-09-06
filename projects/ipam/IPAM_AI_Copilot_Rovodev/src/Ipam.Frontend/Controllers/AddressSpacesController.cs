using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Models;
using Ipam.DataAccess;
using Ipam.Frontend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    [Route("api/addressspaces")]
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
        public async Task<IActionResult> Create([FromBody] AddressSpaceCreateModel model)
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
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Gets an address space by ID
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
        public async Task<IActionResult> GetById(string id)
        {
            var addressSpace = await _dataAccessService.GetAddressSpaceAsync(id);
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
        public async Task<IActionResult> GetAll([FromQuery] AddressSpaceQueryModel query)
        {
            var addressSpaces = await _dataAccessService.GetAddressSpacesAsync();
            
            // Apply filtering if needed
            if (!string.IsNullOrEmpty(query.NameFilter) || query.CreatedAfter.HasValue)
            {
                addressSpaces = addressSpaces.Where(a => 
                    (string.IsNullOrEmpty(query.NameFilter) || a.Name.Contains(query.NameFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (!query.CreatedAfter.HasValue || a.CreatedOn >= query.CreatedAfter.Value));
            }
                
            return Ok(addressSpaces);
        }

        /// <summary>
        /// Updates an existing address space
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AddressSpaceUpdateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get existing address space
            var existingAddressSpace = await _dataAccessService.GetAddressSpaceAsync(id);
            if (existingAddressSpace == null)
                return NotFound();

            // Update properties
            existingAddressSpace.Name = model.Name;
            existingAddressSpace.Description = model.Description;
            existingAddressSpace.ModifiedOn = DateTime.UtcNow;

            var updatedAddressSpace = await _dataAccessService.UpdateAddressSpaceAsync(existingAddressSpace);
            return Ok(updatedAddressSpace);
        }

        /// <summary>
        /// Deletes an address space
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            var existingAddressSpace = await _dataAccessService.GetAddressSpaceAsync(id);
            if (existingAddressSpace == null)
                return NotFound();

            await _dataAccessService.DeleteAddressSpaceAsync(id);
            return NoContent();
        }
    }
}