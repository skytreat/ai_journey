using System.Net.Http.Json;
using IPAM.Contracts;

namespace IPAM.Clients;

public sealed class IpamClient
{
	private readonly HttpClient _http;
	public IpamClient(HttpClient http) => _http = http;

	public async Task<IReadOnlyList<AddressSpaceDto>> GetAddressSpacesAsync(CancellationToken ct = default)
	{
		var res = await _http.GetFromJsonAsync<List<AddressSpaceDto>>("api/v1/address-spaces", ct);
		return res ?? new List<AddressSpaceDto>();
	}
}
