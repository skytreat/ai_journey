using IPAM.Core;
using IPAM.Data;
using Microsoft.AspNetCore.Mvc;

namespace IPAM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressSpaceController : ControllerBase
    {
        private readonly IRepository _repository;

        public AddressSpaceController(IRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AddressSpace>> GetById(Guid id)
        {
            var addressSpace = await _repository.GetAddressSpaceById(id);
            if (addressSpace == null)
            {
                return NotFound();
            }
            return Ok(addressSpace);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressSpace>>> GetAll(string keyword = null, DateTime? createdAfter = null, DateTime? createdBefore = null)
        {
            var addressSpaces = await _repository.GetAddressSpaces(keyword, createdAfter, createdBefore);
            return Ok(addressSpaces);
        }

        [HttpPost]
        public async Task<ActionResult<AddressSpace>> Create(AddressSpace addressSpace)
        {
            addressSpace.Id = Guid.NewGuid();
            addressSpace.CreatedOn = DateTime.UtcNow;
            addressSpace.ModifiedOn = DateTime.UtcNow;
            await _repository.AddAddressSpace(addressSpace);
            return CreatedAtAction(nameof(GetById), new { id = addressSpace.Id }, addressSpace);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, AddressSpace addressSpace)
        {
            if (id != addressSpace.Id)
            {
                return BadRequest();
            }

            addressSpace.ModifiedOn = DateTime.UtcNow;
            await _repository.UpdateAddressSpace(addressSpace);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _repository.DeleteAddressSpace(id);
            return NoContent();
        }
    }
}