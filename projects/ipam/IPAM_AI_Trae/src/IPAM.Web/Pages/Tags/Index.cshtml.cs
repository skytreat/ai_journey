using IPAM.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace IPAM.Web.Pages.Tags
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public List<Tag>? Tags { get; set; }
        public List<SelectListItem> TagTypes { get; set; } = new List<SelectListItem>();
        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }
        [BindProperty(SupportsGet = true)]
        public TagType? TagType { get; set; }

        public async Task OnGetAsync()
        {
            TagTypes = Enum.GetValues(typeof(TagType))
                .Cast<TagType>()
                .Select(t => new SelectListItem(t.ToString(), t.ToString()))
                .ToList();

            var query = new List<string>();
            if (!string.IsNullOrEmpty(SearchString))
                query.Add($"searchString={SearchString}");
            if (TagType.HasValue)
                query.Add($"tagType={TagType}");

            var queryString = query.Any() ? $"?{string.Join("&", query)}" : "";
            Tags = await _httpClient.GetFromJsonAsync<List<Tag>>($"api/tag{queryString}");
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/tag/{id}");
            response.EnsureSuccessStatusCode();
            return RedirectToPage();
        }
    }
}