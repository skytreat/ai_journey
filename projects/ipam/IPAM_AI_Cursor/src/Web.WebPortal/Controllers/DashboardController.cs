using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Web.WebPortal.Controllers;

public class DashboardController : Controller
{
	private readonly IHttpClientFactory _httpFactory;
	private readonly IConfiguration _config;
	
	public DashboardController(IHttpClientFactory httpFactory, IConfiguration config)
	{
		_httpFactory = httpFactory;
		_config = config;
	}

	public async Task<IActionResult> Index()
	{
		var baseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:5080";
		var http = _httpFactory.CreateClient();
		
		try
		{
			// Get address spaces count
			var addressSpaces = await http.GetFromJsonAsync<dynamic>($"{baseUrl}/api/v1/address-spaces?pageSize=1") ?? new { totalCount = 0 };
			
			// For demo purposes, we'll create some mock statistics
			// In a real implementation, you'd have dedicated endpoints for these
			var stats = new DashboardStats
			{
				TotalAddressSpaces = addressSpaces.totalCount ?? 0,
				TotalTags = 0, // Would come from a dedicated stats endpoint
				TotalIpAddresses = 0, // Would come from a dedicated stats endpoint
				RecentActivity = new List<string>
				{
					"Address space 'Production Network' created",
					"Tag 'Environment:Production' added to Production Network",
					"IP range 192.168.1.0/24 allocated",
					"Address space 'Development Network' created"
				}
			};
			
			return View(stats);
		}
		catch
		{
			// Return mock data if API is not available
			var stats = new DashboardStats
			{
				TotalAddressSpaces = 0,
				TotalTags = 0,
				TotalIpAddresses = 0,
				RecentActivity = new List<string>
				{
					"System initialized",
					"Ready for configuration"
				}
			};
			
			return View(stats);
		}
	}
}

public class DashboardStats
{
	public int TotalAddressSpaces { get; set; }
	public int TotalTags { get; set; }
	public int TotalIpAddresses { get; set; }
	public List<string> RecentActivity { get; set; } = new();
}
