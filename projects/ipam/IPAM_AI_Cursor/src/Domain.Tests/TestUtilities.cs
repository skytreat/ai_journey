using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace Domain.Tests;

public static class TestUtilities
{
    public static async Task<string> CreateTestAddressSpace(HttpClient client, string name = "Test Network", string cidr = "192.168.1.0/24")
    {
        var addressSpace = new
        {
            Name = name,
            Description = $"Test network: {name}",
            Cidr = cidr
        };

        var json = JsonSerializer.Serialize(addressSpace);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/address-spaces", content);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<dynamic>(responseContent);
        return created.GetProperty("id").GetString();
    }

    public static async Task<string> CreateTestTag(HttpClient client, string name = "Test Tag", string type = "Inheritable")
    {
        var tag = new
        {
            Name = name,
            Description = $"Test tag: {name}",
            Type = type,
            KnownValues = new[] { "Value1", "Value2" }
        };

        var json = JsonSerializer.Serialize(tag);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/tags", content);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<dynamic>(responseContent);
        return created.GetProperty("id").GetString();
    }

    public static async Task<string> CreateTestIpAddress(HttpClient client, string cidr = "192.168.100.0/24", string name = "Test IP Range")
    {
        var ipAddress = new
        {
            Cidr = cidr,
            Name = name,
            Description = $"Test IP range: {name}"
        };

        var json = JsonSerializer.Serialize(ipAddress);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/ip-addresses", content);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<dynamic>(responseContent);
        return created.GetProperty("id").GetString();
    }

    public static async Task CleanupTestData(HttpClient client, List<string> ids, string endpoint)
    {
        foreach (var id in ids)
        {
            try
            {
                await client.DeleteAsync($"{endpoint}/{id}");
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    public static async Task<int> GetTotalCount(HttpClient client, string endpoint)
    {
        var response = await client.GetAsync($"{endpoint}?pageSize=1&pageNumber=1");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(content);
        return result.GetProperty("totalCount").GetInt32();
    }

    public static async Task AssertPaginatedResponse(HttpClient client, string endpoint, int expectedPageSize)
    {
        var response = await client.GetAsync($"{endpoint}?pageSize={expectedPageSize}&pageNumber=1");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(content);
        
        result.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(expectedPageSize);
        result.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        result.GetProperty("pageNumber").GetInt32().Should().Be(1);
        result.GetProperty("pageSize").GetInt32().Should().Be(expectedPageSize);
    }
}
