using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using IPAM.Contracts;

namespace Web.WebPortal.Controllers;

public class TagsController : Controller
{
	private readonly IHttpClientFactory _httpFactory;
	private readonly IConfiguration _config;
	public TagsController(IHttpClientFactory httpFactory, IConfiguration config)
	{
		_httpFactory = httpFactory; _config = config;
	}

	public async Task<IActionResult> Index(Guid addressSpaceId, int pageNumber = 1, int pageSize = 20)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var url = $"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/tags?pageNumber={pageNumber}&pageSize={pageSize}";
		var result = await http.GetFromJsonAsync<PaginatedResult<TagVm>>(url) ?? new PaginatedResult<TagVm>(new(), 0, 1, 20, 0);
		ViewBag.AddressSpaceId = addressSpaceId; ViewBag.Error = TempData["Error"]; ViewBag.Success = TempData["Success"]; 
		ViewBag.Pagination = result;
		return View(result.Items);
	}

	[HttpGet]
	public IActionResult Create(Guid addressSpaceId)
	{
		ViewBag.AddressSpaceId = addressSpaceId;
		return View(new TagCreateVm());
	}

	[HttpPost]
	public async Task<IActionResult> Create(Guid addressSpaceId, TagCreateVm vm)
	{
		if (!ModelState.IsValid) { ViewBag.AddressSpaceId = addressSpaceId; return View(vm); }
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var payload = new
		{
			addressSpaceId,
			name = vm.Name,
			description = vm.Description,
			type = vm.Type
		};
		var resp = await http.PutAsJsonAsync($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/tags", payload);
		if (!resp.IsSuccessStatusCode)
		{
			var msg = await resp.Content.ReadAsStringAsync();
			ModelState.AddModelError(string.Empty, msg); ViewBag.AddressSpaceId = addressSpaceId; return View(vm);
		}
		TempData["Success"] = "Tag created.";
		return RedirectToAction("Index", new { addressSpaceId });
	}

	[HttpGet]
	public async Task<IActionResult> Edit(Guid addressSpaceId, string name)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var tag = await http.GetFromJsonAsync<TagDetailVm>($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/tags/{name}");
		if (tag == null) return NotFound();
		ViewBag.AddressSpaceId = addressSpaceId;
		return View(new TagEditVm { OriginalName = tag.Name, Name = tag.Name, Description = tag.Description, Type = tag.Type });
	}

	[HttpPost]
	public async Task<IActionResult> Edit(Guid addressSpaceId, TagEditVm vm)
	{
		if (!ModelState.IsValid) { ViewBag.AddressSpaceId = addressSpaceId; return View(vm); }
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var payload = new
		{
			addressSpaceId,
			name = vm.Name,
			description = vm.Description,
			type = vm.Type
		};
		var resp = await http.PutAsJsonAsync($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/tags", payload);
		if (!resp.IsSuccessStatusCode)
		{
			var msg = await resp.Content.ReadAsStringAsync();
			ModelState.AddModelError(string.Empty, msg); ViewBag.AddressSpaceId = addressSpaceId; return View(vm);
		}
		if (!string.Equals(vm.OriginalName, vm.Name, StringComparison.OrdinalIgnoreCase))
		{
			await http.DeleteAsync($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/tags/{vm.OriginalName}");
		}
		TempData["Success"] = "Tag updated.";
		return RedirectToAction("Index", new { addressSpaceId });
	}

	[HttpPost]
	public async Task<IActionResult> Delete(Guid addressSpaceId, string name)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var resp = await http.DeleteAsync($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/tags/{name}");
		if (!resp.IsSuccessStatusCode)
			TempData["Error"] = await resp.Content.ReadAsStringAsync();
		else TempData["Success"] = "Tag deleted.";
		return RedirectToAction("Index", new { addressSpaceId });
	}
}

public sealed class TagVm
{
	public Guid AddressSpaceId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Type { get; set; } = string.Empty;
}

public sealed class TagDetailVm
{
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Type { get; set; } = string.Empty;
}

public sealed class TagCreateVm
{
	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;
	[StringLength(1000)]
	public string? Description { get; set; }
	[Required]
	public string Type { get; set; } = "Inheritable";
}

public sealed class TagEditVm
{
	public string OriginalName { get; set; } = string.Empty;
	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;
	[StringLength(1000)]
	public string? Description { get; set; }
	[Required]
	public string Type { get; set; } = "Inheritable";
}
