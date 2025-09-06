using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ipam.DataAccess;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Api.Models;
using AutoMapper;

namespace Ipam.DataAccess.Api.Controllers
{
    /// <summary>
    /// Controller for IP Address data operations
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [ApiController]
    [Route("api/addressspaces/{addressSpaceId}/[controller]")]
    [Authorize]
    public class IPAddressesController : ControllerBase
    {
        private readonly IDataAccessService _dataAccessService;
        private readonly IMapper _mapper;
        private readonly ILogger<IPAddressesController> _logger;

        public IPAddressesController(
            IDataAccessService dataAccessService,
            IMapper mapper,
            ILogger<IPAddressesController> logger)
        {
            _dataAccessService = dataAccessService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get IP addresses with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IPAddressDto>>> GetIPAddresses(
            string addressSpaceId,
            [FromQuery] string? cidr = null,
            [FromQuery] Dictionary<string, string>? tags = null)
        {
            try
            {
                var ipAddresses = await _dataAccessService.GetIPAddressesAsync(addressSpaceId, cidr, tags);
                var dtos = _mapper.Map<IEnumerable<IPAddressDto>>(ipAddresses);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IP addresses for address space {AddressSpaceId}", addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get IP address by ID
        /// </summary>
        [HttpGet("{ipId}")]
        public async Task<ActionResult<IPAddressDto>> GetIPAddress(string addressSpaceId, string ipId)
        {
            try
            {
                var ipAddress = await _dataAccessService.GetIPAddressAsync(addressSpaceId, ipId);
                if (ipAddress == null)
                {
                    return NotFound();
                }

                var dto = _mapper.Map<IPAddressDto>(ipAddress);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IP address {IpId} in address space {AddressSpaceId}", ipId, addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create new IP address
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<IPAddressDto>> CreateIPAddress(string addressSpaceId, CreateIPAddressDto createDto)
        {
            try
            {
                var ipAddress = _mapper.Map<IPAddress>(createDto);
                ipAddress.AddressSpaceId = addressSpaceId;
                ipAddress.Id = Guid.NewGuid().ToString();
                
                var created = await _dataAccessService.CreateIPAddressAsync(ipAddress);
                var dto = _mapper.Map<IPAddressDto>(created);
                
                return CreatedAtAction(nameof(GetIPAddress), 
                    new { addressSpaceId, ipId = created.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating IP address in address space {AddressSpaceId}", addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update IP address
        /// </summary>
        [HttpPut("{ipId}")]
        public async Task<ActionResult<IPAddressDto>> UpdateIPAddress(
            string addressSpaceId, 
            string ipId, 
            UpdateIPAddressDto updateDto)
        {
            try
            {
                var existing = await _dataAccessService.GetIPAddressAsync(addressSpaceId, ipId);
                if (existing == null)
                {
                    return NotFound();
                }

                _mapper.Map(updateDto, existing);
                var updated = await _dataAccessService.UpdateIPAddressAsync(existing);
                var dto = _mapper.Map<IPAddressDto>(updated);
                
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating IP address {IpId} in address space {AddressSpaceId}", ipId, addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete IP address
        /// </summary>
        [HttpDelete("{ipId}")]
        public async Task<ActionResult> DeleteIPAddress(string addressSpaceId, string ipId)
        {
            try
            {
                var existing = await _dataAccessService.GetIPAddressAsync(addressSpaceId, ipId);
                if (existing == null)
                {
                    return NotFound();
                }

                await _dataAccessService.DeleteIPAddressAsync(addressSpaceId, ipId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting IP address {IpId} in address space {AddressSpaceId}", ipId, addressSpaceId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}