using IPAM.Core;
using IPAM.Data;
using Microsoft.AspNetCore.Mvc;

namespace IPAM.API.Controllers
{
    [ApiController]
    [Route("api/address-spaces/{addressSpaceId}/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly IRepository _repository;

        public TagController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<Tag>> GetById(Guid addressSpaceId, string name)
        {
            var tag = await _repository.GetTagById(addressSpaceId, name);
            if (tag == null)
            {
                return NotFound();
            }
            return Ok(tag);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tag>>> GetAll(Guid addressSpaceId, string keyword = null)
        {
            var tags = await _repository.GetTags(addressSpaceId, keyword);
            return Ok(tags);
        }

        [HttpPost]
        public async Task<ActionResult<Tag>> Create(Guid addressSpaceId, Tag tag)
        {
            tag.AddressSpaceId = addressSpaceId;
            tag.CreatedOn = DateTime.UtcNow;
            tag.ModifiedOn = DateTime.UtcNow;
            await _repository.AddTag(tag);
            return CreatedAtAction(nameof(GetById), new { addressSpaceId, name = tag.Name }, tag);
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> Update(Guid addressSpaceId, string name, Tag tag)
        {
            if (addressSpaceId != tag.AddressSpaceId || name != tag.Name)
            {
                return BadRequest();
            }

            tag.ModifiedOn = DateTime.UtcNow;
            await _repository.UpdateTag(tag);
            return NoContent();
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(Guid addressSpaceId, string name)
        {
            await _repository.DeleteTag(addressSpaceId, name);
            return NoContent();
        }
    }
}