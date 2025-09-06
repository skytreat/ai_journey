using IPAM.Application;
using IPAM.Domain;
using IPAM.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Services.Frontend.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/address-spaces")]
public class AddressSpacesController : ControllerBase
{
	private readonly IAddressSpaceRepository _repo;
	public AddressSpacesController(IAddressSpaceRepository repo) => _repo = repo;

	[HttpGet]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> Query([FromQuery] string? name, [FromQuery] DateTimeOffset? createdAfter, [FromQuery] DateTimeOffset? createdBefore, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
	{
		var pagination = new PaginationParameters(pageNumber, pageSize);
		var list = await _repo.QueryAsync(name, createdAfter, createdBefore, ct);
		var totalCount = list.Count;
		var pagedItems = list.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
		var result = new PaginatedResult<AddressSpace>(pagedItems, totalCount, pagination.PageNumber, pagination.PageSize, (int)Math.Ceiling((double)totalCount / pagination.PageSize));
		return Ok(result);
	}

	[HttpGet("{id:guid}")]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> Get(Guid id, CancellationToken ct)
	{
		var item = await _repo.GetAsync(id, ct);
		return item is null ? NotFound() : Ok(item);
	}

	public sealed record AddressSpaceInput(string? Name, string? Description);

	[HttpPost]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Create([FromBody] AddressSpaceInput input, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest("Name is required.");
		var entity = new AddressSpace
		{
			Id = Guid.NewGuid(),
			Name = input.Name!.Trim(),
			Description = input.Description?.Trim(),
			CreatedOn = DateTimeOffset.UtcNow,
			ModifiedOn = DateTimeOffset.UtcNow
		};
		await _repo.CreateAsync(entity, ct);
		return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
	}

	[HttpPut("{id:guid}")]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Update(Guid id, [FromBody] AddressSpaceInput input, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest("Name is required.");
		var cur = await _repo.GetAsync(id, ct);
		if (cur is null) return NotFound();
		cur.Name = input.Name!.Trim();
		cur.Description = input.Description?.Trim();
		cur.ModifiedOn = DateTimeOffset.UtcNow;
		await _repo.UpdateAsync(cur, ct);
		return NoContent();
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
	{
		await _repo.DeleteAsync(id, ct);
		return NoContent();
	}
}
