using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ipam.DataAccess.Client.Models;
using Ipam.DataAccess.Client.Configuration;

namespace Ipam.DataAccess.Client
{
    /// <summary>
    /// HTTP client for Data Access API
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class DataAccessApiClient : IDataAccessApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly DataAccessApiOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public DataAccessApiClient(HttpClient httpClient, IOptions<DataAccessApiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            }
        }

        // Address Space operations
        public async Task<IEnumerable<AddressSpaceDto>> GetAddressSpacesAsync()
        {
            var response = await _httpClient.GetAsync("api/addressspaces");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<AddressSpaceDto>>(json, _jsonOptions) ?? new List<AddressSpaceDto>();
        }

        public async Task<AddressSpaceDto?> GetAddressSpaceAsync(string id)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AddressSpaceDto>(json, _jsonOptions);
        }

        public async Task<AddressSpaceDto> CreateAddressSpaceAsync(CreateAddressSpaceDto createDto)
        {
            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/addressspaces", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AddressSpaceDto>(responseJson, _jsonOptions)!;
        }

        public async Task<AddressSpaceDto> UpdateAddressSpaceAsync(string id, UpdateAddressSpaceDto updateDto)
        {
            var json = JsonSerializer.Serialize(updateDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/addressspaces/{id}", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AddressSpaceDto>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteAddressSpaceAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"api/addressspaces/{id}");
            response.EnsureSuccessStatusCode();
        }

        // IP Address operations
        public async Task<IEnumerable<IPAddressDto>> GetIPAddressesAsync(string addressSpaceId, string? cidr = null, Dictionary<string, string>? tags = null)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(cidr))
                queryParams.Add($"cidr={Uri.EscapeDataString(cidr)}");
            
            if (tags != null)
            {
                foreach (var tag in tags)
                    queryParams.Add($"tags[{Uri.EscapeDataString(tag.Key)}]={Uri.EscapeDataString(tag.Value)}");
            }

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/ipaddresses{query}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<IPAddressDto>>(json, _jsonOptions) ?? new List<IPAddressDto>();
        }

        public async Task<IPAddressDto?> GetIPAddressAsync(string addressSpaceId, string ipId)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/ipaddresses/{ipId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IPAddressDto>(json, _jsonOptions);
        }

        public async Task<IPAddressDto> CreateIPAddressAsync(string addressSpaceId, CreateIPAddressDto createDto)
        {
            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/addressspaces/{addressSpaceId}/ipaddresses", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IPAddressDto>(responseJson, _jsonOptions)!;
        }

        public async Task<IPAddressDto> UpdateIPAddressAsync(string addressSpaceId, string ipId, UpdateIPAddressDto updateDto)
        {
            var json = JsonSerializer.Serialize(updateDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/addressspaces/{addressSpaceId}/ipaddresses/{ipId}", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IPAddressDto>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteIPAddressAsync(string addressSpaceId, string ipId)
        {
            var response = await _httpClient.DeleteAsync($"api/addressspaces/{addressSpaceId}/ipaddresses/{ipId}");
            response.EnsureSuccessStatusCode();
        }

        // Tag operations
        public async Task<IEnumerable<TagDto>> GetTagsAsync(string addressSpaceId)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/tags");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<TagDto>>(json, _jsonOptions) ?? new List<TagDto>();
        }

        public async Task<TagDto?> GetTagAsync(string addressSpaceId, string tagName)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/tags/{Uri.EscapeDataString(tagName)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TagDto>(json, _jsonOptions);
        }

        public async Task<TagDto> CreateTagAsync(string addressSpaceId, CreateTagDto createDto)
        {
            var json = JsonSerializer.Serialize(createDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/addressspaces/{addressSpaceId}/tags", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TagDto>(responseJson, _jsonOptions)!;
        }

        public async Task<TagDto> UpdateTagAsync(string addressSpaceId, string tagName, UpdateTagDto updateDto)
        {
            var json = JsonSerializer.Serialize(updateDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/addressspaces/{addressSpaceId}/tags/{Uri.EscapeDataString(tagName)}", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TagDto>(responseJson, _jsonOptions)!;
        }

        public async Task DeleteTagAsync(string addressSpaceId, string tagName)
        {
            var response = await _httpClient.DeleteAsync($"api/addressspaces/{addressSpaceId}/tags/{Uri.EscapeDataString(tagName)}");
            response.EnsureSuccessStatusCode();
        }

        // Health check
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}