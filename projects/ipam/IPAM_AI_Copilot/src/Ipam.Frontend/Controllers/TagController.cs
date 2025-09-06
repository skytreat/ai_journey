using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Interfaces;
using Ipam.Frontend.Models;
using System.Threading.Tasks;
using Ipam.DataAccess.Models;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for managing tags
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public TagController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("{addressSpaceId}/{tagName}")]
        public async Task<IActionResult> Get(string addressSpaceId, string tagName)
        {
            var tag = await _unitOfWork.Tags.GetByNameAsync(addressSpaceId, tagName);
            if (tag == null)
                return NotFound();

            return Ok(tag);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TagCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tag = new Tag
            {
                PartitionKey = model.AddressSpaceId,
                RowKey = model.Name,
                Type = model.Type,
                Description = model.Description,
                KnownValues = model.KnownValues,
                Implies = model.Implies
            };

            await _unitOfWork.Tags.CreateAsync(tag);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), 
                new { addressSpaceId = tag.PartitionKey, tagName = tag.RowKey }, 
                tag);
        }
    }
}
