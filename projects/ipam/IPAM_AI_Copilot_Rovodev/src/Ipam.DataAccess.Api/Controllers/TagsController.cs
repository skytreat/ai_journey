using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Api.Models;
using AutoMapper;

namespace Ipam.DataAccess.Api.Controllers
{
    /// <summary>
    /// Controller for Tag data operations
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [ApiController]
    [Route("api/addressspaces/{addressSpaceId}/[controller]")]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly IDataAccessService _dataAccessService;
        private readonly IMapper _mapper;
        private readonly ILogger<TagsController> _logger;

        public TagsController(
            IDataAccessService dataAccessService,
            IMapper mapper,
            ILogger<TagsController> logger)
        {
            _dataAccessService = dataAccessService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get all tags for an address space
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetTags(string addressSpaceId)
        {
            try
            {
                var tags = await _dataAccessService.GetTagsAsync(addressSpaceId);
                var dtos = _mapper.Map<IEnumerable<TagDto>>(tags);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tags for address space {AddressSpaceId}", addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get tag by name
        /// </summary>
        [HttpGet("{tagName}")]
        public async Task<ActionResult<TagDto>> GetTag(string addressSpaceId, string tagName)
        {
            try
            {
                var tag = await _dataAccessService.GetTagAsync(addressSpaceId, tagName);
                if (tag == null)
                {
                    return NotFound();
                }

                var dto = _mapper.Map<TagDto>(tag);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag {TagName} in address space {AddressSpaceId}", tagName, addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create new tag
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TagDto>> CreateTag(string addressSpaceId, CreateTagDto createDto)
        {
            try
            {
                var tag = _mapper.Map<Tag>(createDto);
                var created = await _dataAccessService.CreateTagAsync(addressSpaceId, tag);
                var dto = _mapper.Map<TagDto>(created);
                
                return CreatedAtAction(nameof(GetTag), 
                    new { addressSpaceId, tagName = created.Name }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag in address space {AddressSpaceId}", addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update tag
        /// </summary>
        [HttpPut("{tagName}")]
        public async Task<ActionResult<TagDto>> UpdateTag(
            string addressSpaceId, 
            string tagName, 
            UpdateTagDto updateDto)
        {
            try
            {
                var existing = await _dataAccessService.GetTagAsync(addressSpaceId, tagName);
                if (existing == null)
                {
                    return NotFound();
                }

                _mapper.Map(updateDto, existing);
                var updated = await _dataAccessService.UpdateTagAsync(addressSpaceId, existing);
                var dto = _mapper.Map<TagDto>(updated);
                
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag {TagName} in address space {AddressSpaceId}", tagName, addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete tag
        /// </summary>
        [HttpDelete("{tagName}")]
        public async Task<ActionResult> DeleteTag(string addressSpaceId, string tagName)
        {
            try
            {
                var existing = await _dataAccessService.GetTagAsync(addressSpaceId, tagName);
                if (existing == null)
                {
                    return NotFound();
                }

                await _dataAccessService.DeleteTagAsync(addressSpaceId, tagName);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {TagName} in address space {AddressSpaceId}", tagName, addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}