
using System;
using System.Threading.Tasks;
using Ipam.DataAccess.Interfaces;
using Ipam.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ipam.Frontend.Controllers
{
    [ApiController]
    [Route("api/v1/addressspaces/{addressSpaceId}/tags")]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly ITagRepository _tagRepository;

        public TagsController(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        [HttpGet]
        [Authorize(Policy = "AddressSpaceViewer")]
        public async Task<IActionResult> GetTags(Guid addressSpaceId)
        {
            var tags = await _tagRepository.GetTagsAsync(addressSpaceId);
            return Ok(tags);
        }

        [HttpGet("{name}")]
        [Authorize(Policy = "AddressSpaceViewer")]
        public async Task<IActionResult> GetTag(Guid addressSpaceId, string name)
        {
            var tag = await _tagRepository.GetTagAsync(addressSpaceId, name);
            if (tag == null)
            {
                return NotFound();
            }
            return Ok(tag);
        }

        [HttpPost]
        [Authorize(Policy = "AddressSpaceAdmin")]
        public async Task<IActionResult> CreateTag(Guid addressSpaceId, [FromBody] TagDto tagDto)
        {
            var tag = new Core.Tag
            {
                AddressSpaceId = addressSpaceId,
                Name = tagDto.Name,
                Description = tagDto.Description,
                Type = (Core.TagType)tagDto.Type,
                KnownValues = tagDto.KnownValues,
                Attributes = tagDto.Attributes,
                CreatedOn = DateTimeOffset.UtcNow,
                ModifiedOn = DateTimeOffset.UtcNow
            };

            var createdTag = await _tagRepository.CreateTagAsync(tag);
            return CreatedAtAction(nameof(GetTag), new { addressSpaceId = createdTag.AddressSpaceId, name = createdTag.Name }, createdTag);
        }

        [HttpPut("{name}")]
        [Authorize(Policy = "AddressSpaceAdmin")]
        public async Task<IActionResult> UpdateTag(Guid addressSpaceId, string name, [FromBody] TagDto tagDto)
        {
            var tag = await _tagRepository.GetTagAsync(addressSpaceId, name);
            if (tag == null)
            {
                return NotFound();
            }

            tag.Description = tagDto.Description;
            tag.Type = (Core.TagType)tagDto.Type;
            tag.KnownValues = tagDto.KnownValues;
            tag.Attributes = tagDto.Attributes;
            tag.ModifiedOn = DateTimeOffset.UtcNow;

            var updatedTag = await _tagRepository.UpdateTagAsync(tag);
            return Ok(updatedTag);
        }

        [HttpDelete("{name}")]
        [Authorize(Policy = "AddressSpaceAdmin")]
        public async Task<IActionResult> DeleteTag(Guid addressSpaceId, string name)
        {
            var tag = await _tagRepository.GetTagAsync(addressSpaceId, name);
            if (tag == null)
            {
                return NotFound();
            }

            await _tagRepository.DeleteTagAsync(addressSpaceId, name);
            return NoContent();
        }
    }
}
