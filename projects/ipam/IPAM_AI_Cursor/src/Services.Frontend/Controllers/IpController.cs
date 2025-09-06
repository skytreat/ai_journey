using IPAM.Application;
using IPAM.Domain;
using IPAM.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Services.Frontend.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/address-spaces/{addressSpaceId:guid}/ips")]
public class IpController : ControllerBase
{
	private readonly IIpRepository _repo;
	private readonly ICidrService _cidr;
	public IpController(IIpRepository repo) { _repo = repo; _cidr = new IPAM.Infrastructure.BasicCidrService(); }

	[HttpGet("{id:guid}")]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> GetById(Guid addressSpaceId, Guid id, CancellationToken ct)
	{
		var ip = await _repo.GetByIdAsync(addressSpaceId, id, ct);
		return ip is null ? NotFound() : Ok(ip);
	}

	[HttpGet("by-cidr/{cidr}")]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> GetByCidr(Guid addressSpaceId, string cidr, CancellationToken ct)
	{
		var ip = await _repo.GetByCidrAsync(addressSpaceId, cidr, ct);
		return ip is null ? NotFound() : Ok(ip);
	}

	[HttpGet("{id:guid}/children")]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> Children(Guid addressSpaceId, Guid id, CancellationToken ct)
	{
		var list = await _repo.GetChildrenAsync(addressSpaceId, id, ct);
		return Ok(list);
	}

	public sealed record IpInput(string? Prefix);
	[HttpGet]
	[Authorize(Policy = "AddressSpaceViewer")]
	public async Task<IActionResult> Query(Guid addressSpaceId, [FromQuery] string? tagName, [FromQuery] string? tagValue, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
	{
		var pagination = new PaginationParameters(pageNumber, pageSize);
		var dict = new Dictionary<string,string>();
		if (!string.IsNullOrWhiteSpace(tagName) && !string.IsNullOrWhiteSpace(tagValue)) dict[tagName] = tagValue;
		var list = await _repo.QueryByTagsAsync(addressSpaceId, dict, ct);
		var totalCount = list.Count;
		var pagedItems = list.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
		var result = new PaginatedResult<IpCidr>(pagedItems, totalCount, pagination.PageNumber, pagination.PageSize, (int)Math.Ceiling((double)totalCount / pagination.PageSize));
		return Ok(result);
	}

	[HttpPost]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Upsert(Guid addressSpaceId, [FromBody] IpInput input, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(input.Prefix) || !_cidr.IsValidCidr(input.Prefix)) return BadRequest("Valid CIDR prefix is required.");
		var ip = new IpCidr
		{
			AddressSpaceId = addressSpaceId,
			Id = Guid.NewGuid(),
			Prefix = input.Prefix.Trim(),
			CreatedOn = DateTimeOffset.UtcNow,
			ModifiedOn = DateTimeOffset.UtcNow
		};
		await _repo.UpsertAsync(ip, ct);
		return Ok(ip);
	}

	[HttpPut("{id:guid}")]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Update(Guid addressSpaceId, Guid id, [FromBody] IpInput input, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(input.Prefix) || !_cidr.IsValidCidr(input.Prefix)) return BadRequest("Valid CIDR prefix is required.");
		var cur = await _repo.GetByIdAsync(addressSpaceId, id, ct);
		if (cur is null) return NotFound();
		cur.Prefix = input.Prefix.Trim();
		cur.ModifiedOn = DateTimeOffset.UtcNow;
		await _repo.UpsertAsync(cur, ct);
		return NoContent();
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Policy = "AddressSpaceAdmin")]
	public async Task<IActionResult> Delete(Guid addressSpaceId, Guid id, CancellationToken ct)
	{
		await _repo.DeleteAsync(addressSpaceId, id, ct);
		return NoContent();
	}
}
