using IPAM.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IPAM.Web.Pages.AddressSpaces
{
    public class EditModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public EditModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        [BindProperty]
        public AddressSpace? AddressSpace { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            AddressSpace = await _httpClient.GetFromJsonAsync<AddressSpace>($"api/addressspace/{id}");
            if (AddressSpace == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var response = await _httpClient.PutAsJsonAsync($"api/addressspace/{AddressSpace.Id}", AddressSpace);
            response.EnsureSuccessStatusCode();

            return RedirectToPage("./Index");
        }
    }
}