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
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }
        /// <summary>
        /// Gets a specific tag by name within an address space
        /// </summary>
        [HttpGet("{tagName}")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "addressSpaceId", "tagName" })]
        public async Task<ActionResult<Tag>> GetById(string addressSpaceId, string tagName)
        {
            var tag = await _tagService.GetTagAsync(addressSpaceId, tagName);
            if (tag == null)
                return NotFound();

            return Ok(tag);
        }

        [HttpGet]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "addressSpaceId" })]
        public async Task<ActionResult<IEnumerable<Tag>>> GetAll(string addressSpaceId)
        {
            var tags = await _tagService.GetTagsAsync(addressSpaceId);
            return Ok(tags);
        }

        [HttpPost]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Create(string addressSpaceId, [FromBody] TagCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.AddressSpaceId != addressSpaceId)
                return BadRequest("Address space ID mismatch between route and model.");

            var newTag = new Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = model.Name,
                Description = model.Description,
                Type = model.Type,
                KnownValues = model.KnownValues?.ToList() ?? new List<string>(),
                Implies = model.Implies ?? new Dictionary<string, Dictionary<string, string>>(),
                Attributes = model.Attributes ?? new Dictionary<string, Dictionary<string, string>>(),
            };

            var result = await _tagService.CreateTagAsync(newTag);
            return CreatedAtAction(nameof(GetById), 
                new { addressSpaceId = addressSpaceId, tagName = result.Name }, 
                result);
        }

        [HttpPut("{tagName}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Update(string addressSpaceId, string tagName, [FromBody] TagUpdateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tagToUpdate = new Tag
            {
                Description = model.Description,
                Type = model.Type,
                KnownValues = model.KnownValues?.ToList() ?? new List<string>(),
                Implies = model.Implies ?? new Dictionary<string, Dictionary<string, string>>(),
                Attributes = model.Attributes ?? new Dictionary<string, Dictionary<string, string>>(),
                Name = tagName,
                AddressSpaceId = addressSpaceId,
                ModifiedOn = DateTime.UtcNow
            };
            var updatedTag = await _tagService.UpdateTagAsync(tagToUpdate);
            if (updatedTag == null)
            {
                return NotFound();
            }
            return Ok(updatedTag);
        }

        [HttpDelete("{tagName}")]
        [Authorize(Roles = "SystemAdmin,AddressSpaceAdmin")]
        public async Task<IActionResult> Delete(string addressSpaceId, string tagName)
        {
            var existingTag = await _tagService.GetTagAsync(addressSpaceId, tagName);
            if (existingTag == null)
                return NotFound();

            await _tagService.DeleteTagAsync(addressSpaceId, tagName);
            return NoContent();
        }
    }
}
