using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Api.Models;
using AutoMapper;

namespace Ipam.DataAccess.Api.Controllers
{
    /// <summary>
    /// Controller for Address Space data operations
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressSpacesController : ControllerBase
    {
        private readonly IDataAccessService _dataAccessService;
        private readonly IMapper _mapper;
        private readonly ILogger<AddressSpacesController> _logger;

        public AddressSpacesController(
            IDataAccessService dataAccessService,
            IMapper mapper,
            ILogger<AddressSpacesController> logger)
        {
            _dataAccessService = dataAccessService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get all address spaces
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressSpaceDto>>> GetAddressSpaces()
        {
            try
            {
                var addressSpaces = await _dataAccessService.GetAddressSpacesAsync();
                var dtos = _mapper.Map<IEnumerable<AddressSpaceDto>>(addressSpaces);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving address spaces");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get address space by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressSpaceDto>> GetAddressSpace(string id)
        {
            try
            {
                var addressSpace = await _dataAccessService.GetAddressSpaceAsync(id);
                if (addressSpace == null)
                {
                    return NotFound();
                }

                var dto = _mapper.Map<AddressSpaceDto>(addressSpace);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving address space {AddressSpaceId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create new address space
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AddressSpaceDto>> CreateAddressSpace(CreateAddressSpaceDto createDto)
        {
            try
            {
                var addressSpace = _mapper.Map<AddressSpace>(createDto);
                var created = await _dataAccessService.CreateAddressSpaceAsync(addressSpace);
                var dto = _mapper.Map<AddressSpaceDto>(created);
                
                return CreatedAtAction(nameof(GetAddressSpace), new { id = created.RowKey }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating address space");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update address space
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AddressSpaceDto>> UpdateAddressSpace(string id, UpdateAddressSpaceDto updateDto)
        {
            try
            {
                var existing = await _dataAccessService.GetAddressSpaceAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }

                _mapper.Map(updateDto, existing);
                var updated = await _dataAccessService.UpdateAddressSpaceAsync(existing);
                var dto = _mapper.Map<AddressSpaceDto>(updated);
                
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address space {AddressSpaceId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete address space
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAddressSpace(string id)
        {
            try
            {
                var existing = await _dataAccessService.GetAddressSpaceAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }

                await _dataAccessService.DeleteAddressSpaceAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address space {AddressSpaceId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}