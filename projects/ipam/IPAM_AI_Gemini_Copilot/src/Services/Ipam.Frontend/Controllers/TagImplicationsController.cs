
using System;
using System.Threading.Tasks;
using Ipam.DataAccess.Interfaces;
using Ipam.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ipam.Frontend.Controllers
{
    [ApiController]
    [Route("api/v1/addressspaces/{addressSpaceId}/tagimplications")]
    [Authorize(Policy = "AddressSpaceAdmin")]
    public class TagImplicationsController : ControllerBase
    {
        private readonly ITagImplicationRepository _tagImplicationRepository;

        public TagImplicationsController(ITagImplicationRepository tagImplicationRepository)
        {
            _tagImplicationRepository = tagImplicationRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetTagImplications(Guid addressSpaceId)
        {
            var tagImplications = await _tagImplicationRepository.GetTagImplicationsAsync(addressSpaceId);
            return Ok(tagImplications);
        }

        [HttpGet("{ifTagValue}")]
        public async Task<IActionResult> GetTagImplication(Guid addressSpaceId, string ifTagValue)
        {
            var tagImplication = await _tagImplicationRepository.GetTagImplicationAsync(addressSpaceId, ifTagValue);
            if (tagImplication == null)
            {
                return NotFound();
            }
            return Ok(tagImplication);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTagImplication(Guid addressSpaceId, [FromBody] TagImplicationDto tagImplicationDto)
        {
            var tagImplication = new Core.TagImplication
            {
                AddressSpaceId = addressSpaceId,
                IfTagValue = tagImplicationDto.IfTagValue,
                ThenTagValue = tagImplicationDto.ThenTagValue
            };

            var createdTagImplication = await _tagImplicationRepository.CreateTagImplicationAsync(tagImplication);
            return CreatedAtAction(nameof(GetTagImplication), new { addressSpaceId = createdTagImplication.AddressSpaceId, ifTagValue = createdTagImplication.IfTagValue }, createdTagImplication);
        }

        [HttpDelete("{ifTagValue}")]
        public async Task<IActionResult> DeleteTagImplication(Guid addressSpaceId, string ifTagValue)
        {
            var tagImplication = await _tagImplicationRepository.GetTagImplicationAsync(addressSpaceId, ifTagValue);
            if (tagImplication == null)
            {
                return NotFound();
            }

            await _tagImplicationRepository.DeleteTagImplicationAsync(addressSpaceId, ifTagValue);
            return NoContent();
        }
    }
}
