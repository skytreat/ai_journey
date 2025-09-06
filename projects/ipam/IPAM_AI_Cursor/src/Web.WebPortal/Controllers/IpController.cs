using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using IPAM.Contracts;

namespace Web.WebPortal.Controllers;

public class IpController : Controller
{
	private readonly IHttpClientFactory _httpFactory;
	private readonly IConfiguration _config;
	public IpController(IHttpClientFactory httpFactory, IConfiguration config)
	{
		_httpFactory = httpFactory; _config = config;
	}

	public async Task<IActionResult> Index(Guid addressSpaceId, int pageNumber = 1, int pageSize = 20)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var url = $"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/ips?pageNumber={pageNumber}&pageSize={pageSize}";
		var result = await http.GetFromJsonAsync<PaginatedResult<IpVm>>(url) ?? new PaginatedResult<IpVm>(new(), 0, 1, 20, 0);
		ViewBag.AddressSpaceId = addressSpaceId; ViewBag.Error = TempData["Error"]; ViewBag.Success = TempData["Success"]; 
		ViewBag.Pagination = result;
		return View(result.Items);
	}

	[HttpGet]
	public IActionResult Create(Guid addressSpaceId)
	{
		ViewBag.AddressSpaceId = addressSpaceId;
		return View(new IpCreateVm());
	}

	[HttpPost]
	public async Task<IActionResult> Create(Guid addressSpaceId, IpCreateVm vm)
	{
		if (!ModelState.IsValid) { ViewBag.AddressSpaceId = addressSpaceId; return View(vm); }
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var payload = new { prefix = vm.Prefix };
		var resp = await http.PostAsJsonAsync($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/ips", payload);
		if (!resp.IsSuccessStatusCode)
		{
			var msg = await resp.Content.ReadAsStringAsync();
			ModelState.AddModelError(string.Empty, msg); ViewBag.AddressSpaceId = addressSpaceId; return View(vm);
		}
		TempData["Success"] = "IP created.";
		return RedirectToAction("Index", new { addressSpaceId });
	}

	[HttpGet]
	public async Task<IActionResult> Edit(Guid addressSpaceId, Guid id)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var ip = await http.GetFromJsonAsync<IpVm>($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/ips/{id}");
		if (ip == null) return NotFound();
		ViewBag.AddressSpaceId = addressSpaceId;
		return View(new IpEditVm { Id = ip.Id, Prefix = ip.Prefix });
	}

	[HttpPost]
	public async Task<IActionResult> Edit(Guid addressSpaceId, IpEditVm vm)
	{
		if (!ModelState.IsValid) { ViewBag.AddressSpaceId = addressSpaceId; return View(vm); }
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var payload = new { id = vm.Id, addressSpaceId, prefix = vm.Prefix };
		var resp = await http.PutAsJsonAsync($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/ips/{vm.Id}", payload);
		if (!resp.IsSuccessStatusCode)
		{
			var msg = await resp.Content.ReadAsStringAsync();
			ModelState.AddModelError(string.Empty, msg); ViewBag.AddressSpaceId = addressSpaceId; return View(vm);
		}
		TempData["Success"] = "IP updated.";
		return RedirectToAction("Index", new { addressSpaceId });
	}

	[HttpPost]
	public async Task<IActionResult> Delete(Guid addressSpaceId, Guid id)
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		var resp = await http.DeleteAsync($"{baseUrl}/api/v1/address-spaces/{addressSpaceId}/ips/{id}");
		if (!resp.IsSuccessStatusCode)
			TempData["Error"] = await resp.Content.ReadAsStringAsync();
		else TempData["Success"] = "IP deleted.";
		return RedirectToAction("Index", new { addressSpaceId });
	}
}

public sealed class IpVm
{
	public Guid Id { get; set; }
	public string Prefix { get; set; } = string.Empty;
}

public sealed class IpCreateVm
{
	[Required]
	[RegularExpression(@"^([0-9]{1,3}\.){3}[0-9]{1,3}\/[0-9]{1,2}$|^[0-9a-fA-F:]+\/[0-9]{1,3}$", ErrorMessage = "CIDR format required.")]
	[StringLength(100)]
	public string Prefix { get; set; } = string.Empty;
}

public sealed class IpEditVm
{
	public Guid Id { get; set; }
	[Required]
	[RegularExpression(@"^([0-9]{1,3}\.){3}[0-9]{1,3}\/[0-9]{1,2}$|^[0-9a-fA-F:]+\/[0-9]{1,3}$", ErrorMessage = "CIDR format required.")]
	[StringLength(100)]
	public string Prefix { get; set; } = string.Empty;
}
