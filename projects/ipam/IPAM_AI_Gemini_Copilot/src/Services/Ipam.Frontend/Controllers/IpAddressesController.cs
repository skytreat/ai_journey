
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Dto;
using Ipam.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ipam.Frontend.Controllers
{
    [ApiController]
    [Route("api/v1/addressspaces/{addressSpaceId}/ips")]
    [Authorize]
    public class IpAddressesController : ControllerBase
    {
        private readonly IIpamService _ipamService;
        private readonly DataAccess.Interfaces.IIpAddressRepository _ipAddressRepository; // Keep for GetIpAddresses and GetChildren

        public IpAddressesController(IIpamService ipamService, DataAccess.Interfaces.IIpAddressRepository ipAddressRepository)
        {
            _ipamService = ipamService;
            _ipAddressRepository = ipAddressRepository;
        }

        [HttpGet]
        [Authorize(Policy = "AddressSpaceViewer")]
        public async Task<IActionResult> GetIpAddresses(Guid addressSpaceId, [FromQuery] string cidr, [FromQuery] Dictionary<string, string> tags)
        {
            var ipAddresses = await _ipAddressRepository.GetIpAddressesAsync(addressSpaceId, cidr, tags);
            return Ok(ipAddresses);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AddressSpaceViewer")]
        public async Task<IActionResult> GetIpAddress(Guid addressSpaceId, Guid id)
        {
            var ipAddress = await _ipamService.GetIpAddressWithInheritedTagsAsync(addressSpaceId, id);
            if (ipAddress == null)
            {
                return NotFound();
            }
            return Ok(ipAddress);
        }

        [HttpPost]
        [Authorize(Policy = "AddressSpaceAdmin")]
        public async Task<IActionResult> CreateIpAddress(Guid addressSpaceId, [FromBody] IpAddressDto ipAddressDto)
        {
            var ipAddress = new Core.IpAddress
            {
                Id = Guid.NewGuid(),
                AddressSpaceId = addressSpaceId,
                Prefix = ipAddressDto.Prefix,
                Tags = ipAddressDto.Tags,
                ParentId = ipAddressDto.ParentId,
                CreatedOn = DateTimeOffset.UtcNow,
                ModifiedOn = DateTimeOffset.UtcNow
            };

            var createdIpAddress = await _ipamService.CreateIpAddressAsync(ipAddress);
            return CreatedAtAction(nameof(GetIpAddress), new { addressSpaceId = createdIpAddress.AddressSpaceId, id = createdIpAddress.Id }, createdIpAddress);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AddressSpaceAdmin")]
        public async Task<IActionResult> UpdateIpAddress(Guid addressSpaceId, Guid id, [FromBody] IpAddressDto ipAddressDto)
        {
            var ipAddress = new Core.IpAddress
            {
                Id = id,
                AddressSpaceId = addressSpaceId,
                Prefix = ipAddressDto.Prefix,
                Tags = ipAddressDto.Tags,
                ParentId = ipAddressDto.ParentId,
                ModifiedOn = DateTimeOffset.UtcNow
            };

            var updatedIpAddress = await _ipamService.UpdateIpAddressAsync(ipAddress);
            return Ok(updatedIpAddress);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AddressSpaceAdmin")]
        public async Task<IActionResult> DeleteIpAddress(Guid addressSpaceId, Guid id)
        {
            await _ipamService.DeleteIpAddressAsync(addressSpaceId, id);
            return NoContent();
        }

        [HttpGet("{id}/children")]
        [Authorize(Policy = "AddressSpaceViewer")]
        public async Task<IActionResult> GetChildren(Guid addressSpaceId, Guid id)
        {
            var children = await _ipAddressRepository.GetChildrenAsync(addressSpaceId, id);
            return Ok(children);
        }
    }
}
