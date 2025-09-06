using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ipam.DataAccess.Interfaces;
using Ipam.Frontend.Models;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Ipam.DataAccess.Models;
using System.Collections.Generic;

namespace Ipam.Frontend.Controllers
{
    /// <summary>
    /// Controller for managing IP nodes
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IpNodeController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public IpNodeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get IP node by address space ID and IP ID
        /// </summary>
        [HttpGet("{addressSpaceId}/{ipId}")]
        public async Task<IActionResult> Get(string addressSpaceId, string ipId)
        {
            var ipNode = await _unitOfWork.IpNodes.GetByIdAsync(addressSpaceId, ipId);
            if (ipNode == null)
                return NotFound();

            return Ok(ipNode);
        }

        /// <summary>
        /// Create a new IP node
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IpNodeCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipNode = new IpNode
            {
                PartitionKey = model.AddressSpaceId,
                RowKey = Guid.NewGuid().ToString(),
                Prefix = model.Prefix,
                Tags = model.Tags ?? new Dictionary<string, string>()
            };

            await _unitOfWork.IpNodes.CreateAsync(ipNode);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), 
                new { addressSpaceId = ipNode.PartitionKey, ipId = ipNode.RowKey }, 
                ipNode);
        }

        /// <summary>
        /// Update an existing IP node
        /// </summary>
        [HttpPut("{addressSpaceId}/{ipId}")]
        public async Task<IActionResult> Update(string addressSpaceId, string ipId, 
            [FromBody] IpNodeUpdateModel model)
        {
            var ipNode = await _unitOfWork.IpNodes.GetByIdAsync(addressSpaceId, ipId);
            if (ipNode == null)
                return NotFound();

            ipNode.Prefix = model.Prefix;
            ipNode.Tags = model.Tags;

            await _unitOfWork.IpNodes.UpdateAsync(ipNode);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ipNode);
        }

        /// <summary>
        /// Delete an IP node
        /// </summary>
        [HttpDelete("{addressSpaceId}/{ipId}")]
        public async Task<IActionResult> Delete(string addressSpaceId, string ipId)
        {
            var ipNode = await _unitOfWork.IpNodes.GetByIdAsync(addressSpaceId, ipId);
            if (ipNode == null)
                return NotFound();

            await _unitOfWork.IpNodes.DeleteAsync(addressSpaceId, ipId);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get IP nodes by prefix
        /// </summary>
        [HttpGet("byPrefix/{addressSpaceId}")]
        public async Task<IActionResult> GetByPrefix(string addressSpaceId, [FromQuery] string prefix)
        {
            var ipNodes = await _unitOfWork.IpNodes.GetByPrefixAsync(addressSpaceId, prefix);
            return Ok(ipNodes);
        }

        /// <summary>
        /// Get IP nodes by tags
        /// </summary>
        [HttpGet("byTags/{addressSpaceId}")]
        public async Task<IActionResult> GetByTags(string addressSpaceId, [FromQuery] Dictionary<string, string> tags)
        {
            var ipNodes = await _unitOfWork.IpNodes.GetByTagsAsync(addressSpaceId, tags);
            return Ok(ipNodes);
        }

        /// <summary>
        /// Get child nodes of an IP node
        /// </summary>
        [HttpGet("{addressSpaceId}/{ipId}/children")]
        public async Task<IActionResult> GetChildren(string addressSpaceId, string ipId)
        {
            var children = await _unitOfWork.IpNodes.GetChildrenAsync(addressSpaceId, ipId);
            return Ok(children);
        }
    }
}
