
using System;
using System.Threading.Tasks;
using Ipam.DataAccess.Interfaces;
using Ipam.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ipam.Frontend.Controllers
{
    [ApiController]
    [Route("api/v1/addressspaces")]
    [Authorize]
    public class AddressSpacesController : ControllerBase
    {
        private readonly IAddressSpaceRepository _addressSpaceRepository;

        public AddressSpacesController(IAddressSpaceRepository addressSpaceRepository)
        {
            _addressSpaceRepository = addressSpaceRepository;
        }

        [HttpGet]
        [Authorize(Policy = "AddressSpaceViewer")]
        public async Task<IActionResult> GetAddressSpaces()
        {
            var addressSpaces = await _addressSpaceRepository.GetAddressSpacesAsync();
            return Ok(addressSpaces);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AddressSpaceViewer")]
        public async Task<IActionResult> GetAddressSpace(Guid id)
        {
            var addressSpace = await _addressSpaceRepository.GetAddressSpaceAsync(id);
            if (addressSpace == null)
            {
                return NotFound();
            }
            return Ok(addressSpace);
        }

        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> CreateAddressSpace([FromBody] AddressSpaceDto addressSpaceDto)
        {
            var addressSpace = new Core.AddressSpace
            {
                Id = Guid.NewGuid(),
                Name = addressSpaceDto.Name,
                Description = addressSpaceDto.Description,
                CreatedOn = DateTimeOffset.UtcNow,
                ModifiedOn = DateTimeOffset.UtcNow
            };

            var createdAddressSpace = await _addressSpaceRepository.CreateAddressSpaceAsync(addressSpace);
            return CreatedAtAction(nameof(GetAddressSpace), new { id = createdAddressSpace.Id }, createdAddressSpace);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AddressSpaceAdmin")]
        public async Task<IActionResult> UpdateAddressSpace(Guid id, [FromBody] AddressSpaceDto addressSpaceDto)
        {
            var addressSpace = await _addressSpaceRepository.GetAddressSpaceAsync(id);
            if (addressSpace == null)
            {
                return NotFound();
            }

            addressSpace.Name = addressSpaceDto.Name;
            addressSpace.Description = addressSpaceDto.Description;
            addressSpace.ModifiedOn = DateTimeOffset.UtcNow;

            var updatedAddressSpace = await _addressSpaceRepository.UpdateAddressSpaceAsync(addressSpace);
            return Ok(updatedAddressSpace);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> DeleteAddressSpace(Guid id)
        {
            var addressSpace = await _addressSpaceRepository.GetAddressSpaceAsync(id);
            if (addressSpace == null)
            {
                return NotFound();
            }

            await _addressSpaceRepository.DeleteAddressSpaceAsync(id);
            return NoContent();
        }
    }
}
