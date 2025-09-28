using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ipam.ServiceContract.DTOs;
using Ipam.ServiceContract.Interfaces;
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
        private readonly IAddressSpaceService _addressSpaceService;

        public AddressSpacesController(IAddressSpaceService addressSpaceService)
        {
            _addressSpaceService = addressSpaceService;
        }

        /// <summary>
        /// Creates a new address space
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Create([FromBody] AddressSpaceCreateModel model)
        {
            if (model == null)
                return BadRequest("Model cannot be null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!string.IsNullOrEmpty(model.Id))
            {
                var existingAddressSpace = await _addressSpaceService.GetAddressSpaceByIdAsync(model.Id);
                if (existingAddressSpace != null)
                {
                    return Ok(existingAddressSpace);
                }
            }

            var newAddressSpace = new AddressSpace
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description
            };

            var result = await _addressSpaceService.CreateAddressSpaceAsync(newAddressSpace);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Gets an address space by ID
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<AddressSpace>> GetById(string id)
        {
            var addressSpace = await _addressSpaceService.GetAddressSpaceByIdAsync(id);
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
        public async Task<ActionResult<IEnumerable<AddressSpace>>> GetAll([FromQuery] AddressSpaceQueryModel query)
        {
            var addressSpaces = await _addressSpaceService.GetAddressSpacesAsync();
            
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
            if (model == null)
                return BadRequest("Model cannot be null.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var addressSpaceToUpdate = new AddressSpace
            {
                Id = id,
                Name = model.Name,
                Description = model.Description
            };

            var updatedAddressSpace = await _addressSpaceService.UpdateAddressSpaceAsync(addressSpaceToUpdate);
            if (updatedAddressSpace == null)
            {
                return NotFound();
            }
            return Ok(updatedAddressSpace);
        }

        /// <summary>
        /// Deletes an address space
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            var existingAddressSpace = await _addressSpaceService.GetAddressSpaceByIdAsync(id);
            if (existingAddressSpace == null)
                return NotFound();

            await _addressSpaceService.DeleteAddressSpaceAsync(id);
            return NoContent();
        }
    }
}