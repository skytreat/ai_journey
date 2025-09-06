using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using IPAM.Contracts;

namespace Web.WebPortal.Controllers;

public class HomeController : Controller
{
	private readonly IHttpClientFactory _httpFactory;
	private readonly IConfiguration _config;
	public HomeController(IHttpClientFactory httpFactory, IConfiguration config)
	{
		_httpFactory = httpFactory; _config = config;
	}

	public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var url = $"{baseUrl}/api/v1/address-spaces?pageNumber={pageNumber}&pageSize={pageSize}";
		var result = await http.GetFromJsonAsync<PaginatedResult<AddressSpaceVm>>(url) ?? new PaginatedResult<AddressSpaceVm>(new(), 0, 1, 20, 0);
		ViewBag.Error = TempData["Error"]; ViewBag.Success = TempData["Success"]; 
		ViewBag.Pagination = result;
		return View(result.Items);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View(new AddressSpaceEditVm());
	}

	[HttpPost]
	public async Task<IActionResult> Create(AddressSpaceEditVm vm)
	{
		if (!ModelState.IsValid) return View(vm);
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		try
		{
			var payload = new { name = vm.Name, description = vm.Description };
			var resp = await http.PostAsJsonAsync($"{baseUrl}/api/v1/address-spaces", payload);
			if (!resp.IsSuccessStatusCode)
			{
				var msg = await resp.Content.ReadAsStringAsync();
				ModelState.AddModelError(string.Empty, $"Create failed: {msg}");
				return View(vm);
			}
			TempData["Success"] = "Address space created.";
			return RedirectToAction("Index");
		}
		catch (Exception ex)
		{
			ModelState.AddModelError(string.Empty, ex.Message);
			return View(vm);
		}
	}

	[HttpGet]
	public async Task<IActionResult> Edit(Guid id)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var aspace = await http.GetFromJsonAsync<AddressSpaceVm>($"{baseUrl}/api/v1/address-spaces/{id}");
		if (aspace == null) return NotFound();
		return View(new AddressSpaceEditVm { Id = aspace.Id, Name = aspace.Name, Description = aspace.Description });
	}

	[HttpPost]
	public async Task<IActionResult> Edit(AddressSpaceEditVm vm)
	{
		if (!ModelState.IsValid) return View(vm);
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		try
		{
			var payload = new { id = vm.Id, name = vm.Name, description = vm.Description };
			var resp = await http.PutAsJsonAsync($"{baseUrl}/api/v1/address-spaces/{vm.Id}", payload);
			if (!resp.IsSuccessStatusCode)
			{
				var msg = await resp.Content.ReadAsStringAsync();
				ModelState.AddModelError(string.Empty, $"Update failed: {msg}");
				return View(vm);
			}
			TempData["Success"] = "Address space updated.";
			return RedirectToAction("Index");
		}
		catch (Exception ex)
		{
			ModelState.AddModelError(string.Empty, ex.Message);
			return View(vm);
		}
	}

	[HttpPost]
	public async Task<IActionResult> Delete(Guid id)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var resp = await http.DeleteAsync($"{baseUrl}/api/v1/address-spaces/{id}");
		if (!resp.IsSuccessStatusCode)
		{
			TempData["Error"] = $"Delete failed: {await resp.Content.ReadAsStringAsync()}";
		}
		else
		{
			TempData["Success"] = "Address space deleted.";
		}
		return RedirectToAction("Index");
	}
}

public sealed class AddressSpaceVm
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
}

public sealed class AddressSpaceEditVm
{
	public Guid? Id { get; set; }
	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;
	[StringLength(1000)]
	public string? Description { get; set; }
}
