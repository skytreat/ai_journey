using IPAM.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IPAM.Web.Pages.AddressSpaces
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public List<AddressSpace>? AddressSpaces { get; set; }

        public async Task OnGetAsync()
        {
            AddressSpaces = await _httpClient.GetFromJsonAsync<List<AddressSpace>>("api/addressspace");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/addressspace/{id}");
            response.EnsureSuccessStatusCode();
            return RedirectToPage();
        }
    }
}