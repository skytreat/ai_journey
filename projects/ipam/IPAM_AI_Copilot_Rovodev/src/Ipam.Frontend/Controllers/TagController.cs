using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Ipam.Frontend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for managing tags within address spaces
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/addressspaces/{addressSpaceId}/tags")]
    public class TagController : ControllerBase
    {
        private readonly IDataAccessService _dataAccessService;

        public TagController(IDataAccessService dataAccessService)
        {
            _dataAccessService = dataAccessService;
        }

        /// <summary>
        /// Gets a specific tag by name within an address space
        /// </summary>
        [HttpGet("{tagName}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "addressSpaceId", "tagName" })]
        public async Task<IActionResult> GetById(string addressSpaceId, string tagName)
        {
            var tag = await _dataAccessService.GetTagAsync(addressSpaceId, tagName);
            if (tag == null)
                return NotFound();

            return Ok(tag);
        }

        /// <summary>
        /// Gets all tags within an address space
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "addressSpaceId" })]
        public async Task<IActionResult> GetAll(string addressSpaceId)
        {
            var tags = await _dataAccessService.GetTagsAsync(addressSpaceId);
            return Ok(tags);
        }

        /// <summary>
        /// Creates a new tag within an address space
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Create(string addressSpaceId, [FromBody] TagCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ensure the model's address space ID matches the route parameter
            if (model.AddressSpaceId != addressSpaceId)
                return BadRequest("Address space ID mismatch between route and model.");

            var tag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = model.Name,
                Type = model.Type,
                Description = model.Description,
                KnownValues = model.KnownValues,
                Implies = model.Implies,
                Attributes = model.Attributes,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            var result = await _dataAccessService.CreateTagAsync(addressSpaceId, tag);
            return CreatedAtAction(nameof(GetById), 
                new { addressSpaceId = addressSpaceId, tagName = result.Name }, 
                result);
        }

        /// <summary>
        /// Updates an existing tag within an address space
        /// </summary>
        [HttpPut("{tagName}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Update(string addressSpaceId, string tagName, [FromBody] TagCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get existing tag
            var existingTag = await _dataAccessService.GetTagAsync(addressSpaceId, tagName);
            if (existingTag == null)
                return NotFound();

            // Update properties
            existingTag.Description = model.Description;
            existingTag.Type = model.Type;
            existingTag.KnownValues = model.KnownValues;
            existingTag.Implies = model.Implies;
            existingTag.Attributes = model.Attributes;
            existingTag.ModifiedOn = DateTime.UtcNow;

            var updatedTag = await _dataAccessService.UpdateTagAsync(addressSpaceId, existingTag);
            return Ok(updatedTag);
        }

        /// <summary>
        /// Deletes a tag from an address space
        /// </summary>
        [HttpDelete("{tagName}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Delete(string addressSpaceId, string tagName)
        {
            var existingTag = await _dataAccessService.GetTagAsync(addressSpaceId, tagName);
            if (existingTag == null)
                return NotFound();

            await _dataAccessService.DeleteTagAsync(addressSpaceId, tagName);
            return NoContent();
        }
    }
}
