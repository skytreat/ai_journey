using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Interfaces;
using Ipam.Frontend.Models;
using System.Threading.Tasks;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for managing address spaces
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class AddressSpaceController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public AddressSpaceController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var addressSpace = await _unitOfWork.AddressSpaces.GetByIdAsync(id, id);
            if (addressSpace == null)
                return NotFound();

            return Ok(addressSpace);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddressSpaceCreateModel model)
        {
            // Implementation
            return Ok();
        }
    }
}
