using Ipam.DataAccess.Client.Models;

namespace Ipam.DataAccess.Client
{
    /// <summary>
    /// Interface for Data Access API client
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public interface IDataAccessApiClient
    {
        // Address Space operations
        Task<IEnumerable<AddressSpaceDto>> GetAddressSpacesAsync();
        Task<AddressSpaceDto?> GetAddressSpaceAsync(string id);
        Task<AddressSpaceDto> CreateAddressSpaceAsync(CreateAddressSpaceDto createDto);
        Task<AddressSpaceDto> UpdateAddressSpaceAsync(string id, UpdateAddressSpaceDto updateDto);
        Task DeleteAddressSpaceAsync(string id);

        // IP Address operations
        Task<IEnumerable<IPAddressDto>> GetIPAddressesAsync(string addressSpaceId, string? cidr = null, Dictionary<string, string>? tags = null);
        Task<IPAddressDto?> GetIPAddressAsync(string addressSpaceId, string ipId);
        Task<IPAddressDto> CreateIPAddressAsync(string addressSpaceId, CreateIPAddressDto createDto);
        Task<IPAddressDto> UpdateIPAddressAsync(string addressSpaceId, string ipId, UpdateIPAddressDto updateDto);
        Task DeleteIPAddressAsync(string addressSpaceId, string ipId);

        // Tag operations
        Task<IEnumerable<TagDto>> GetTagsAsync(string addressSpaceId);
        Task<TagDto?> GetTagAsync(string addressSpaceId, string tagName);
        Task<TagDto> CreateTagAsync(string addressSpaceId, CreateTagDto createDto);
        Task<TagDto> UpdateTagAsync(string addressSpaceId, string tagName, UpdateTagDto updateDto);
        Task DeleteTagAsync(string addressSpaceId, string tagName);

        // Health check
        Task<bool> IsHealthyAsync();
    }
}