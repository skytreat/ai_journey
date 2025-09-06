
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Ipam.Dto;
using Newtonsoft.Json;

namespace Ipam.Client
{
    public class IpamClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public IpamClient(string apiBaseUrl, string? token = null)
        {
            _httpClient = new HttpClient();
            _apiBaseUrl = apiBaseUrl;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<IEnumerable<AddressSpaceDto>> GetAddressSpacesAsync()
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces");
            var result = JsonConvert.DeserializeObject<IEnumerable<AddressSpaceDto>>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<AddressSpaceDto> GetAddressSpaceAsync(Guid id)
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces/{id}");
            var result = JsonConvert.DeserializeObject<AddressSpaceDto>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<AddressSpaceDto> CreateAddressSpaceAsync(AddressSpaceDto addressSpace)
        {
            var json = JsonConvert.SerializeObject(addressSpace);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/addressspaces", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AddressSpaceDto>(responseJson);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<AddressSpaceDto> UpdateAddressSpaceAsync(Guid id, AddressSpaceDto addressSpace)
        {
            var json = JsonConvert.SerializeObject(addressSpace);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_apiBaseUrl}/api/v1/addressspaces/{id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AddressSpaceDto>(responseJson);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task DeleteAddressSpaceAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/v1/addressspaces/{id}");
            response.EnsureSuccessStatusCode();
        }

        // Tags
        public async Task<IEnumerable<TagDto>> GetTagsAsync(Guid addressSpaceId)
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tags");
            var result = JsonConvert.DeserializeObject<IEnumerable<TagDto>>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<TagDto> GetTagAsync(Guid addressSpaceId, string name)
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tags/{name}");
            var result = JsonConvert.DeserializeObject<TagDto>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<TagDto> CreateTagAsync(Guid addressSpaceId, TagDto tag)
        {
            var json = JsonConvert.SerializeObject(tag);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tags", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TagDto>(responseJson);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<TagDto> UpdateTagAsync(Guid addressSpaceId, string name, TagDto tag)
        {
            var json = JsonConvert.SerializeObject(tag);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tags/{name}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TagDto>(responseJson);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task DeleteTagAsync(Guid addressSpaceId, string name)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tags/{name}");
            response.EnsureSuccessStatusCode();
        }

        // IP Addresses
        public async Task<IEnumerable<IpAddressDto>> GetIpAddressesAsync(Guid addressSpaceId, string? cidr = null, Dictionary<string, string>? tags = null)
        {
            var queryString = new List<string>();
            if (!string.IsNullOrEmpty(cidr))
            {
                queryString.Add($"cidr={cidr}");
            }
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags.AsEnumerable())
                {
                    queryString.Add($"tags[{tag.Key!}]={tag.Value!}");
                }
            }
            var url = $"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/ips";
            if (queryString.Any())
            {
                url += $"?{string.Join("&", queryString)}";
            }
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<IEnumerable<IpAddressDto>>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<IpAddressDto> GetIpAddressAsync(Guid addressSpaceId, Guid id)
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/ips/{id}");
            var result = JsonConvert.DeserializeObject<IpAddressDto>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<IpAddressDto> CreateIpAddressAsync(Guid addressSpaceId, IpAddressDto ipAddress)
        {
            var json = JsonConvert.SerializeObject(ipAddress);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/ips", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IpAddressDto>(responseJson);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<IpAddressDto> UpdateIpAddressAsync(Guid addressSpaceId, Guid id, IpAddressDto ipAddress)
        {
            var json = JsonConvert.SerializeObject(ipAddress);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/ips/{id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IpAddressDto>(responseJson);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task DeleteIpAddressAsync(Guid addressSpaceId, Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/ips/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<IpAddressDto>> GetIpAddressChildrenAsync(Guid addressSpaceId, Guid id)
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/ips/{id}/children");
            var result = JsonConvert.DeserializeObject<IEnumerable<IpAddressDto>>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        // Authentication
        public async Task<string> LoginAsync(UserDto user)
        {
            var json = JsonConvert.SerializeObject(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/auth/login", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic? result = JsonConvert.DeserializeObject(responseJson);
            if (result == null || result.token == null)
            {
                throw new InvalidOperationException("Login response or token was null.");
            }
            return result.token;
        }

        public async Task RegisterAsync(UserDto user)
        {
            var json = JsonConvert.SerializeObject(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/auth/register", content);
            response.EnsureSuccessStatusCode();
        }

        // Tag Implications
        public async Task<IEnumerable<TagImplicationDto>> GetTagImplicationsAsync(Guid addressSpaceId)
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tagimplications");
            var result = JsonConvert.DeserializeObject<IEnumerable<TagImplicationDto>>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<TagImplicationDto> GetTagImplicationAsync(Guid addressSpaceId, string ifTagValue)
        {
            var response = await _httpClient.GetStringAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tagimplications/{ifTagValue}");
            var result = JsonConvert.DeserializeObject<TagImplicationDto>(response);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task<TagImplicationDto> CreateTagImplicationAsync(Guid addressSpaceId, TagImplicationDto tagImplication)
        {
            var json = JsonConvert.SerializeObject(tagImplication);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tagimplications", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TagImplicationDto>(responseJson);
            return result ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        public async Task DeleteTagImplicationAsync(Guid addressSpaceId, string ifTagValue)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/v1/addressspaces/{addressSpaceId}/tagimplications/{ifTagValue}");
            response.EnsureSuccessStatusCode();
        }
    }
}
