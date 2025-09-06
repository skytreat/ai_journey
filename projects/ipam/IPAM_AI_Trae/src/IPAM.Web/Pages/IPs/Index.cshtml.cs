using IPAM.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace IPAM.Web.Pages.IPs
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public List<IP>? IPs { get; set; }
        public List<SelectListItem> AddressSpaces { get; set; } = new List<SelectListItem>();
        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }
        [BindProperty(SupportsGet = true)]
        public Guid? AddressSpaceId { get; set; }

        public async Task OnGetAsync()
        {
            var addressSpaces = await _httpClient.GetFromJsonAsync<List<AddressSpace>>("api/addressspace");
            AddressSpaces = addressSpaces?.Select(a => new SelectListItem(a.Name, a.Id.ToString())).ToList() ?? new List<SelectListItem>();

            var query = new List<string>();
            if (!string.IsNullOrEmpty(SearchString))
                query.Add($"searchString={SearchString}");
            if (AddressSpaceId.HasValue)
                query.Add($"addressSpaceId={AddressSpaceId}");

            var queryString = query.Any() ? $"?{string.Join("&", query)}" : "";
            IPs = await _httpClient.GetFromJsonAsync<List<IP>>($"api/ip{queryString}");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/ip/{id}");
            response.EnsureSuccessStatusCode();
            return RedirectToPage();
        }
    }
}