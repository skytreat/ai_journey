using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Ipam.DataAccess.Models;
using System.Text.Json;

namespace Ipam.WebPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(HttpClient httpClient, ILogger<HomeController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> AddressSpaces()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5000/api/address-spaces");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var addressSpaces = JsonSerializer.Deserialize<IEnumerable<AddressSpace>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(addressSpaces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching address spaces");
                return View(new List<AddressSpace>());
            }
        }

        public async Task<IActionResult> IPAddresses(string addressSpaceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:5000/api/ip-addresses?addressSpaceId={addressSpaceId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var ipAddresses = JsonSerializer.Deserialize<IEnumerable<IPAddress>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(ipAddresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching IP addresses");
                return View(new List<IPAddress>());
            }
        }

        public async Task<IActionResult> Tags(string addressSpaceId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:5000/api/tags?addressSpaceId={addressSpaceId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var tags = JsonSerializer.Deserialize<IEnumerable<Tag>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tags");
                return View(new List<Tag>());
            }
        }
    }
}