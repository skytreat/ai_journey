using IPAM.Application;
using IPAM.Domain;
using IPAM.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Services.Frontend.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/address-spaces/{addressSpaceId:guid}/tags")]
public class TagsController : ControllerBase
{
	private readonly ITagRepository _repo;
	public TagsController(ITagRepository repo) => _repo = repo;

	[HttpGet]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> Query(Guid addressSpaceId, [FromQuery] string? name, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
	{
		var pagination = new PaginationParameters(pageNumber, pageSize);
		var list = await _repo.QueryAsync(addressSpaceId, name, ct);
		var totalCount = list.Count;
		var pagedItems = list.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
		var result = new PaginatedResult<TagDefinition>(pagedItems, totalCount, pagination.PageNumber, pagination.PageSize, (int)Math.Ceiling((double)totalCount / pagination.PageSize));
		return Ok(result);
	}

	[HttpGet("{name}")]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> Get(Guid addressSpaceId, string name, CancellationToken ct)
	{
		var tag = await _repo.GetAsync(addressSpaceId, name, ct);
		return tag is null ? NotFound() : Ok(tag);
	}

	public sealed record TagInput(string Name, string? Description, string Type);

	[HttpPut]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Upsert(Guid addressSpaceId, [FromBody] TagInput input, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest("Tag name is required.");
		if (!Enum.TryParse<TagType>(input.Type, out var parsed)) return BadRequest("Invalid tag type.");
		var tag = new TagDefinition
		{
			AddressSpaceId = addressSpaceId,
			Name = input.Name.Trim(),
			Description = input.Description?.Trim(),
			Type = parsed,
			ModifiedOn = DateTimeOffset.UtcNow,
			CreatedOn = DateTimeOffset.UtcNow
		};
		await _repo.UpsertAsync(tag, ct);
		return NoContent();
	}

	[HttpDelete("{name}")]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Delete(Guid addressSpaceId, string name, CancellationToken ct)
	{
		await _repo.DeleteAsync(addressSpaceId, name, ct);
		return NoContent();
	}
}
