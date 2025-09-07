using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ipam.DataAccess.Models;

namespace Ipam.Client
{
    /// <summary>
    /// HTTP client for IPAM API operations
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpamApiClient
    {
        private readonly HttpClient _httpClient;

        public IpamApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IpamApiClient(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        // Address Space operations
        public async Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace)
        {
            var response = await _httpClient.PostAsJsonAsync("api/addressspaces", addressSpace);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AddressSpace>();
        }

        public async Task<AddressSpace> GetAddressSpaceAsync(string addressSpaceId)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AddressSpace>();
        }

        public async Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync()
        {
            var response = await _httpClient.GetAsync("api/addressspaces");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<AddressSpace>>();
        }

        public async Task<AddressSpace> UpdateAddressSpaceAsync(string addressSpaceId, AddressSpace addressSpace)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/addressspaces/{addressSpaceId}", addressSpace);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AddressSpace>();
        }

        public async Task DeleteAddressSpaceAsync(string addressSpaceId)
        {
            var response = await _httpClient.DeleteAsync($"api/addressspaces/{addressSpaceId}");
            response.EnsureSuccessStatusCode();
        }

        // IP Address operations
        public async Task<IpAllocation> CreateIPAddressAsync(string addressSpaceId, IpAllocation ipAllocation)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/addressspaces/{addressSpaceId}/ipaddresses", ipAllocation);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IpAllocation>();
        }

        public async Task<IpAllocation> GetIPAddressAsync(string addressSpaceId, string ipId)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/ipaddresses/{ipId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IpAllocation>();
        }

        public async Task<IEnumerable<IpAllocation>> GetIPAddressesAsync(string addressSpaceId, string cidr = null, Dictionary<string, string> tags = null)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(cidr))
                queryParams.Add($"cidr={Uri.EscapeDataString(cidr)}");
            
            if (tags != null)
            {
                foreach (var tag in tags)
                    queryParams.Add($"tags={Uri.EscapeDataString($"{tag.Key}={tag.Value}")}");
            }

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/ipaddresses{query}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<IpAllocation>>();
        }

        public async Task<IpAllocation> UpdateIPAddressAsync(string addressSpaceId, string ipId, IpAllocation ipAllocation)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/addressspaces/{addressSpaceId}/ipaddresses/{ipId}", ipAllocation);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IpAllocation>();
        }

        public async Task DeleteIPAddressAsync(string addressSpaceId, string ipId)
        {
            var response = await _httpClient.DeleteAsync($"api/addressspaces/{addressSpaceId}/ipaddresses/{ipId}");
            response.EnsureSuccessStatusCode();
        }

        // Tag operations
        public async Task<Tag> CreateTagAsync(string addressSpaceId, Tag tag)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/addressspaces/{addressSpaceId}/tags", tag);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Tag>();
        }

        public async Task<Tag> GetTagAsync(string addressSpaceId, string tagName)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/tags/{tagName}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Tag>();
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync(string addressSpaceId)
        {
            var response = await _httpClient.GetAsync($"api/addressspaces/{addressSpaceId}/tags");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Tag>>();
        }

        public async Task<Tag> UpdateTagAsync(string addressSpaceId, string tagName, Tag tag)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/addressspaces/{addressSpaceId}/tags/{tagName}", tag);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Tag>();
        }

        public async Task DeleteTagAsync(string addressSpaceId, string tagName)
        {
            var response = await _httpClient.DeleteAsync($"api/addressspaces/{addressSpaceId}/tags/{tagName}");
            response.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}