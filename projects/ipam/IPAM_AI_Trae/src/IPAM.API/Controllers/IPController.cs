using IPAM.Core;
using IPAM.Data;
using Microsoft.AspNetCore.Mvc;

namespace IPAM.API.Controllers
{
    [ApiController]
    [Route("api/address-spaces/{addressSpaceId}/[controller]")]
    public class IPController : ControllerBase
    {
        private readonly IRepository _repository;

        public IPController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IP>> GetById(Guid addressSpaceId, Guid id)
        {
            var ip = await _repository.GetIpById(addressSpaceId, id);
            if (ip == null)
            {
                return NotFound();
            }
            return Ok(ip);
        }

        [HttpGet("by-prefix/{prefix}")]
        public async Task<ActionResult<IP>> GetByPrefix(Guid addressSpaceId, string prefix)
        {
            var ip = await _repository.GetIpByPrefix(addressSpaceId, prefix);
            if (ip == null)
            {
                return NotFound();
            }
            return Ok(ip);
        }

        [HttpGet("by-tags")]
        public async Task<ActionResult<IEnumerable<IP>>> GetByTags(Guid addressSpaceId, [FromQuery] Dictionary<string, string> tags)
        {
            var ips = await _repository.GetIpsByTags(addressSpaceId, tags);
            return Ok(ips);
        }

        [HttpGet("{parentId}/children")]
        public async Task<ActionResult<IEnumerable<IP>>> GetChildren(Guid addressSpaceId, Guid parentId)
        {
            var ips = await _repository.GetChildIps(addressSpaceId, parentId);
            return Ok(ips);
        }

        [HttpPost]
        public async Task<ActionResult<IP>> Create(Guid addressSpaceId, IP ip)
        {
            ip.AddressSpaceId = addressSpaceId;
            ip.Id = Guid.NewGuid();
            ip.CreatedOn = DateTime.UtcNow;
            ip.ModifiedOn = DateTime.UtcNow;
            await _repository.AddIp(ip);
            return CreatedAtAction(nameof(GetById), new { addressSpaceId, id = ip.Id }, ip);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid addressSpaceId, Guid id, IP ip)
        {
            if (addressSpaceId != ip.AddressSpaceId || id != ip.Id)
            {
                return BadRequest();
            }

            ip.ModifiedOn = DateTime.UtcNow;
            await _repository.UpdateIp(ip);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid addressSpaceId, Guid id)
        {
            await _repository.DeleteIp(addressSpaceId, id);
            return NoContent();
        }
    }
}