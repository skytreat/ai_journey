using Ipam.ServiceContract.DTOs;
using Ipam.ServiceContract.Models;
using System.Collections.Generic;

namespace Ipam.ServiceContract.Interfaces;

public interface IIpAllocationService
{
    // Core CRUD operations
    Task<IpAllocation> CreateIpAllocationAsync(IpAllocation ipAllocation, CancellationToken cancellationToken = default);
    Task<IpAllocation?> GetIpAllocationByIdAsync(string addressSpaceId, string id, CancellationToken cancellationToken = default);
    Task<IList<IpAllocation>> GetIpAllocationsAsync(string addressSpaceId, CancellationToken cancellationToken = default);

    Task<IList<IpAllocation>> GetIpAllocationsByPrefixAsync(string addressSpaceId, Prefix prefix, CancellationToken cancellationToken = default);

    Task<IList<IpAllocation>> GetIpAllocationsByTagsAsync(string addressSpaceId, Dictionary<string, string> tags, CancellationToken cancellationToken = default);
    Task<IpAllocation> UpdateIpAllocationAsync(IpAllocation ipAllocation, CancellationToken cancellationToken = default);
    Task DeleteIpAllocationAsync(string addressSpaceId, string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<IpAllocation>> GetChildrenAsync(string addressSpaceId, string parentId, CancellationToken cancellationToken = default);
    
    // Advanced IP allocation operations
    Task<List<string>> FindAvailableSubnetsAsync(string addressSpaceId, string parentCidr, int subnetSize, int count = 1, CancellationToken cancellationToken = default);
    Task<IpUtilizationStats> CalculateUtilizationAsync(string addressSpaceId, string networkCidr, CancellationToken cancellationToken = default);
    Task<IpAllocation> AllocateNextSubnetAsync(string addressSpaceId, string parentCidr, int subnetSize, Dictionary<string, string> tags = null, CancellationToken cancellationToken = default);
    Task<SubnetValidationResult> ValidateSubnetAllocationAsync(string addressSpaceId, string proposedCidr, CancellationToken cancellationToken = default);
}