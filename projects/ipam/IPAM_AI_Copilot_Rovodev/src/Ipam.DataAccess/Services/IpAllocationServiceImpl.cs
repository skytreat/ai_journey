using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.DTOs;
using Ipam.ServiceContract.Interfaces;
using Ipam.ServiceContract.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Implementation of IP allocation service with business logic
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpAllocationServiceImpl : IIpAllocationService
    {
        private readonly IIpAllocationRepository _ipAllocationRepository;
        private readonly IpTreeService _ipTreeService;
        private readonly ConcurrentIpTreeService _concurrentIpTreeService;
        private readonly PerformanceMonitoringService _performanceService;
        private readonly IMapper _mapper;
        private readonly ILogger<IpAllocationServiceImpl> _logger;

        public IpAllocationServiceImpl(
            IIpAllocationRepository ipNodeRepository,
            IpTreeService ipTreeService,
            ConcurrentIpTreeService concurrentIpTreeService,
            PerformanceMonitoringService performanceService,
            IMapper mapper,
            ILogger<IpAllocationServiceImpl> logger)
        {
            _ipAllocationRepository = ipNodeRepository;
            _ipTreeService = ipTreeService;
            _concurrentIpTreeService = concurrentIpTreeService;
            _performanceService = performanceService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IpAllocation> CreateIpAllocationAsync(IpAllocation ipAllocation, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating IP allocation {IpId} in address space {AddressSpaceId}", 
                    ipAllocation.Id, ipAllocation.AddressSpaceId);

                // Use concurrent tree service for thread-safe creation with business logic
                var entity = await _concurrentIpTreeService.CreateIpAllocationAsync(
                    ipAllocation,
                    cancellationToken);

                var result = _mapper.Map<IpAllocation>(entity);
                result.Id = ipAllocation.Id; // Preserve the original ID
                
                _logger.LogInformation("Successfully created IP allocation {IpId}", ipAllocation.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create IP allocation {IpId} in address space {AddressSpaceId}", 
                    ipAllocation.Id, ipAllocation.AddressSpaceId);
                throw;
            }
        }

        public async Task<IpAllocation?> GetIpAllocationByIdAsync(string addressSpaceId, string id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting IP allocation {IpId} from address space {AddressSpaceId}", id, addressSpaceId);
                
                var entity = await _ipAllocationRepository.GetByIdAsync(addressSpaceId, id);
                if (entity == null)
                {
                    _logger.LogWarning("IP allocation {IpId} not found in address space {AddressSpaceId}", id, addressSpaceId);
                    return null;
                }

                return _mapper.Map<IpAllocation>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get IP allocation {IpId} from address space {AddressSpaceId}", id, addressSpaceId);
                throw;
            }
        }
        
        public async Task<IList<IpAllocation>> GetIpAllocationsByPrefixAsync(string addressSpaceId, Prefix prefix, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting IP allocations from address space {AddressSpaceId} with prefix {Prefix}", addressSpaceId, prefix);

                var entities = await _ipAllocationRepository.GetByPrefixAsync(addressSpaceId, prefix.ToString());

                var result = _mapper.Map<IList<IpAllocation>>(entities);
                _logger.LogDebug("Retrieved {Count} IP allocations from address space {AddressSpaceId} with prefix {Prefix}", 
                    result.Count(), addressSpaceId, prefix);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get IP allocations from address space {AddressSpaceId} with prefix {Prefix}", addressSpaceId, prefix);
                throw;
            }
        }

        public async Task<IList<IpAllocation>> GetIpAllocationsAsync(string addressSpaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting IP allocations from address space {AddressSpaceId}", addressSpaceId);

                var entities = await _ipAllocationRepository.GetAllAsync(addressSpaceId);

                var result = _mapper.Map<IList<IpAllocation>>(entities);
                _logger.LogDebug("Retrieved {Count} IP allocations from address space {AddressSpaceId}",
                    result.Count(), addressSpaceId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get IP allocations from address space {AddressSpaceId}", addressSpaceId);
                throw;
            }
        }
        
        public async Task<IList<IpAllocation>> GetIpAllocationsByTagsAsync(string addressSpaceId, Dictionary<string, string> tags, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting IP allocations from address space {AddressSpaceId} with tags {Tags}", addressSpaceId, tags);

                var entities = await _ipAllocationRepository.GetByTagsAsync(addressSpaceId, tags);

                var result = _mapper.Map<IList<IpAllocation>>(entities);
                _logger.LogDebug("Retrieved {Count} IP allocations from address space {AddressSpaceId} with tags {Tags}", 
                    result.Count(), addressSpaceId, tags);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get IP allocations from address space {AddressSpaceId} with tags {Tags}", addressSpaceId, tags);
                throw;
            }
        }

        public async Task<IpAllocation> UpdateIpAllocationAsync(IpAllocation ipAllocation, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating IP allocation {IpId} in address space {AddressSpaceId}",
                    ipAllocation.Id, ipAllocation.AddressSpaceId);

                var entity = await _ipAllocationRepository.GetByIdAsync(ipAllocation.AddressSpaceId, ipAllocation.Id);
                if (entity == null)
                {
                    _logger.LogWarning("IP allocation {IpId} not found for update in address space {AddressSpaceId}",
                        ipAllocation.Id, ipAllocation.AddressSpaceId);
                    return null;
                }

                // Map updated values while preserving entity metadata
                _mapper.Map(ipAllocation, entity);
                entity.ModifiedOn = DateTime.UtcNow;

                var updatedEntity = await _ipAllocationRepository.UpdateAsync(entity);
                var result = _mapper.Map<IpAllocation>(updatedEntity);

                _logger.LogInformation("Successfully updated IP allocation {IpId}", ipAllocation.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update IP allocation {IpId} in address space {AddressSpaceId}",
                    ipAllocation.Id, ipAllocation.AddressSpaceId);
                throw;
            }
        }

        public async Task DeleteIpAllocationAsync(string addressSpaceId, string id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting IP allocation {IpId}", id);
                
                // Use concurrent tree service for proper cleanup with business logic
                await _concurrentIpTreeService.DeleteIpAllocationAsync(addressSpaceId, id);
                
                _logger.LogInformation("Successfully deleted IP allocation {IpId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete IP allocation {IpId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<IpAllocation>> GetChildrenAsync(string addressSpaceId, string parentId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting children of IP allocation {ParentId}", parentId);
                
                // Use tree service for efficient children retrieval
                var children = await _ipTreeService.GetChildrenAsync(addressSpaceId, parentId);
                var result = _mapper.Map<IEnumerable<IpAllocation>>(children);
                
                _logger.LogDebug("Retrieved {Count} children for IP allocation {ParentId}", 
                    result.Count(), parentId);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get children of IP allocation {ParentId} in address space {AddressSpaceId}", 
                    parentId, addressSpaceId);
                throw;
            }
        }

        public async Task<List<string>> FindAvailableSubnetsAsync(string addressSpaceId, string parentCidr, int subnetSize, int count = 1, CancellationToken cancellationToken = default)
        {
            return await _performanceService.MeasureAsync(
                "FindAvailableSubnets",
                async () =>
                {
                    var parentPrefix = new Prefix(parentCidr);
                    var availableSubnets = new List<string>();

                    // Get all existing IP nodes in the address space
                    var existingNodes = await _ipAllocationRepository.GetChildrenAsync(addressSpaceId, null);
                    var existingPrefixes = existingNodes
                        .Select(n => new Prefix(n.Prefix))
                        .Where(p => p.IsSubnetOf(parentPrefix))
                        .OrderBy(p => p)
                        .ToList();

                    // Generate all possible subnets of the desired size
                    var possibleSubnets = GenerateSubnets(parentPrefix, subnetSize);

                    foreach (var subnet in possibleSubnets)
                    {
                        if (availableSubnets.Count >= count) break;

                        // Check if this subnet conflicts with any existing networks
                        bool isAvailable = !existingPrefixes.Any(existing => 
                            existing.Contains(subnet) || subnet.Contains(existing) || existing.Equals(subnet));

                        if (isAvailable)
                        {
                            availableSubnets.Add(subnet.ToString());
                        }
                    }

                    _logger.LogInformation(
                        "Found {AvailableCount} available subnets out of {RequestedCount} in {ParentCidr}",
                        availableSubnets.Count, count, parentCidr);

                    return availableSubnets;
                },
                new Dictionary<string, object>
                {
                    ["AddressSpaceId"] = addressSpaceId,
                    ["ParentCidr"] = parentCidr,
                    ["SubnetSize"] = subnetSize,
                    ["RequestedCount"] = count
                });
        }

        public async Task<IpUtilizationStats> CalculateUtilizationAsync(string addressSpaceId, string networkCidr, CancellationToken cancellationToken = default)
        {
            return await _performanceService.MeasureAsync(
                "CalculateUtilization",
                async () =>
                {
                    var networkPrefix = new Prefix(networkCidr);
                    var childNodes = await _ipAllocationRepository.GetChildrenAsync(addressSpaceId, null);
                    
                    var subnets = childNodes
                        .Select(n => new Prefix(n.Prefix))
                        .Where(p => networkPrefix.Contains(p))
                        .ToList();

                    var totalAddresses = CalculateTotalAddresses(networkPrefix);
                    var allocatedAddresses = subnets.Sum(s => CalculateTotalAddresses(s));
                    var utilizationPercentage = totalAddresses > 0 ? (double)allocatedAddresses / totalAddresses * 100 : 0;

                    var stats = new IpUtilizationStats
                    {
                        NetworkCidr = networkCidr,
                        TotalAddresses = totalAddresses,
                        AllocatedAddresses = allocatedAddresses,
                        AvailableAddresses = totalAddresses - allocatedAddresses,
                        UtilizationPercentage = utilizationPercentage,
                        SubnetCount = subnets.Count,
                        LargestAvailableBlock = await FindLargestAvailableBlockAsync(addressSpaceId, networkCidr),
                        FragmentationIndex = CalculateFragmentationIndex(subnets, networkPrefix)
                    };

                    _logger.LogInformation(
                        "Utilization for {NetworkCidr}: {Utilization:F2}% ({Allocated}/{Total} addresses)",
                        networkCidr, utilizationPercentage, allocatedAddresses, totalAddresses);

                    return stats;
                },
                new Dictionary<string, object>
                {
                    ["AddressSpaceId"] = addressSpaceId,
                    ["NetworkCidr"] = networkCidr
                });
        }

        public async Task<IpAllocation> AllocateNextSubnetAsync(string addressSpaceId, string parentCidr, int subnetSize, Dictionary<string, string> tags = null, CancellationToken cancellationToken = default)
        {
            var availableSubnets = await FindAvailableSubnetsAsync(addressSpaceId, parentCidr, subnetSize, 1);
            
            if (!availableSubnets.Any())
            {
                throw new InvalidOperationException($"No available subnets of size /{subnetSize} in {parentCidr}");
            }

            var allocatedCidr = availableSubnets.First();
            var entity = await _ipTreeService.CreateIpAllocationAsync(addressSpaceId, allocatedCidr, tags ?? new Dictionary<string, string>());
            return _mapper.Map<IpAllocation>(entity);
        }

        public async Task<SubnetValidationResult> ValidateSubnetAllocationAsync(string addressSpaceId, string proposedCidr, CancellationToken cancellationToken = default)
        {
            var proposedPrefix = new Prefix(proposedCidr);
            var existingNodes = await _ipAllocationRepository.GetChildrenAsync(addressSpaceId, null);
            var conflicts = new List<string>();

            foreach (var node in existingNodes)
            {
                var existingPrefix = new Prefix(node.Prefix);
                
                if (existingPrefix.Contains(proposedPrefix) || 
                    proposedPrefix.Contains(existingPrefix) || 
                    existingPrefix.Equals(proposedPrefix))
                {
                    conflicts.Add(node.Prefix);
                }
            }

            return new SubnetValidationResult
            {
                IsValid = !conflicts.Any(),
                ProposedCidr = proposedCidr,
                ConflictingSubnets = conflicts,
                ValidationMessage = conflicts.Any() 
                    ? $"Subnet {proposedCidr} conflicts with existing allocations: {string.Join(", ", conflicts)}"
                    : "Subnet allocation is valid"
            };
        }

        private List<Prefix> GenerateSubnets(Prefix parentPrefix, int subnetSize)
        {
            var subnets = new List<Prefix>();
            
            if (subnetSize <= parentPrefix.PrefixLength)
                return subnets; // Cannot create larger subnets

            try
            {
                var currentPrefix = parentPrefix;
                var queue = new Queue<Prefix>();
                queue.Enqueue(currentPrefix);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    
                    if (current.PrefixLength == subnetSize)
                    {
                        subnets.Add(current);
                    }
                    else if (current.PrefixLength < subnetSize)
                    {
                        var childSubnets = current.GetSubnets();
                        foreach (var child in childSubnets)
                        {
                            queue.Enqueue(child);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating subnets for {ParentPrefix} with size {SubnetSize}", 
                    parentPrefix, subnetSize);
            }

            return subnets.OrderBy(s => s).ToList();
        }

        private long CalculateTotalAddresses(Prefix prefix)
        {
            var hostBits = (prefix.IsIPv4 ? 32 : 128) - prefix.PrefixLength;
            return hostBits >= 63 ? long.MaxValue : (1L << hostBits);
        }

        private async Task<string> FindLargestAvailableBlockAsync(string addressSpaceId, string networkCidr)
        {
            // Simplified implementation - find the largest gap between allocated subnets
            var availableSubnets = await FindAvailableSubnetsAsync(addressSpaceId, networkCidr, 24, 100);
            return availableSubnets.FirstOrDefault() ?? "None";
        }

        private double CalculateFragmentationIndex(List<Prefix> subnets, Prefix parentPrefix)
        {
            if (!subnets.Any()) return 0.0;

            // Simple fragmentation metric: ratio of allocated blocks to total possible blocks
            var averageSubnetSize = subnets.Average(s => s.PrefixLength);
            var maxPossibleBlocks = Math.Pow(2, averageSubnetSize - parentPrefix.PrefixLength);
            return subnets.Count / maxPossibleBlocks;
        }
    }
}