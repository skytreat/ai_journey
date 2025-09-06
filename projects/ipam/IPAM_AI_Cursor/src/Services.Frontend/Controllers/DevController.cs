using IPAM.Application;
using IPAM.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Services.Frontend.Controllers;

[ApiController]
[Route("api/v1/dev")] 
public class DevController : ControllerBase
{
	private readonly IAddressSpaceRepository _repo;
	private readonly IWebHostEnvironment _env;
	private readonly IConfiguration _config;
	public DevController(IAddressSpaceRepository repo, IWebHostEnvironment env, IConfiguration config)
	{
		_repo = repo; _env = env; _config = config;
	}

	[HttpPost("seed")]
	public async Task<IActionResult> Seed(CancellationToken ct)
	{
		if (!_env.IsDevelopment() || !_config.GetValue<bool>("DevAuth:Enabled")) return NotFound();
		var sample = new AddressSpace
		{
			Id = Guid.NewGuid(),
			Name = "AS-Dev",
			Description = "Dev seeded",
			CreatedOn = DateTimeOffset.UtcNow,
			ModifiedOn = DateTimeOffset.UtcNow
		};
		await _repo.CreateAsync(sample, ct);
		return Ok(sample);
	}
}
