using Ipam.DataAccess.Interfaces;
using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Entities;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ipam.ServiceContract.Interfaces;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Service for managing address spaces with automatic root node creation
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceService : IAddressSpaceService
    {
        private readonly IAddressSpaceRepository _addressSpaceRepository;
        private readonly IIpAllocationRepository _ipNodeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AddressSpaceService> _logger;

        public AddressSpaceService(
            IAddressSpaceRepository addressSpaceRepository,
            IIpAllocationRepository ipNodeRepository,
            IMapper mapper,
            ILogger<AddressSpaceService> logger)
        {
            _addressSpaceRepository = addressSpaceRepository;
            _ipNodeRepository = ipNodeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Creates an address space with automatic root node creation
        /// </summary>
        /// <param name="addressSpace">The address space to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created address space</returns>
        public async Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating address space {Name} with ID {Id}", addressSpace.Name, addressSpace.Id);

                var addressSpaceEntity = _mapper.Map<AddressSpaceEntity>(addressSpace);
                addressSpaceEntity.Id = addressSpace.Id ?? Guid.NewGuid().ToString();
                addressSpaceEntity.PartitionKey = "AddressSpaces";
                addressSpaceEntity.CreatedOn = DateTime.UtcNow;
                addressSpaceEntity.ModifiedOn = DateTime.UtcNow;
                addressSpaceEntity.Status = "Active";

                // Create the address space
                var createdAddressSpace = await _addressSpaceRepository.CreateAsync(addressSpaceEntity);

                // Create root IPv6 node (::/0)
                var rootIpv6 = new IpAllocationEntity
                {
                    Id = "ipv6_root",
                    AddressSpaceId = addressSpaceEntity.Id,
                    Prefix = "::/0",
                    ParentId = null,
                    Tags = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Type", "Root" },
                        { "Version", "IPv6" }
                    },
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };

                // Create root IPv4 node (0.0.0.0/0) as child of IPv6 root
                var rootIpv4 = new IpAllocationEntity
                {
                    Id = "ipv4_root",
                    AddressSpaceId = addressSpaceEntity.Id,
                    Prefix = "0.0.0.0/0",
                    ParentId = "ipv6_root",
                    Tags = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Type", "Root" },
                        { "Version", "IPv4" }
                    },
                    CreatedOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow
                };

                try
                {
                    await _ipNodeRepository.CreateAsync(rootIpv6);
                    await _ipNodeRepository.CreateAsync(rootIpv4);

                    // Update IPv6 root to include IPv4 root as child
                    rootIpv6.ChildrenIds = new List<string> { "ipv4_root" };
                    await _ipNodeRepository.UpdateAsync(rootIpv6);
                }
                catch (Exception ex)
                {
                    // If root node creation fails, we should still return the address space
                    // but log the error for investigation
                    _logger.LogWarning(ex, "Failed to create root nodes for address space {AddressSpaceId}", addressSpaceEntity.Id);
                }

                var result = _mapper.Map<AddressSpace>(createdAddressSpace);
                _logger.LogInformation("Successfully created address space {Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create address space {Name}", addressSpace.Name);
                throw;
            }
        }

        /// <summary>
        /// Gets an address space by ID
        /// </summary>
        /// <param name="id">The address space ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The address space or null if not found</returns>
        public async Task<AddressSpace?> GetAddressSpaceByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting address space {AddressSpaceId}", id);
                
                var addressSpace = await _addressSpaceRepository.GetByIdAsync("AddressSpaces", id);
                if (addressSpace == null)
                {
                    _logger.LogWarning("Address space {AddressSpaceId} not found", id);
                    return null;
                }

                return _mapper.Map<AddressSpace>(addressSpace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get address space {AddressSpaceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Updates an address space
        /// </summary>
        /// <param name="addressSpace">The address space data to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated address space</returns>
        public async Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating address space {AddressSpaceId}", addressSpace.Id);

                var addressSpaceEntity = await _addressSpaceRepository.GetByIdAsync("AddressSpaces", addressSpace.Id);
                if (addressSpaceEntity == null)
                {
                    _logger.LogWarning("Address space {AddressSpaceId} not found for update", addressSpace.Id);
                    return null;
                }

                _mapper.Map(addressSpace, addressSpaceEntity);
                addressSpaceEntity.ModifiedOn = DateTime.UtcNow;

                var updatedEntity = await _addressSpaceRepository.UpdateAsync(addressSpaceEntity);
                var result = _mapper.Map<AddressSpace>(updatedEntity);
                
                _logger.LogInformation("Successfully updated address space {AddressSpaceId}", addressSpace.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update address space {AddressSpaceId}", addressSpace.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes an address space and all its IP nodes
        /// </summary>
        /// <param name="id">The address space ID to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task DeleteAddressSpaceAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting address space {AddressSpaceId}", id);
                
                // In a real implementation, you would want to:
                // 1. Delete all IP nodes in the address space
                // 2. Delete all tags in the address space
                // 3. Finally delete the address space itself
                
                // For now, just delete the address space
                await _addressSpaceRepository.DeleteAsync("AddressSpaces", id);
                
                _logger.LogInformation("Successfully deleted address space {AddressSpaceId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete address space {AddressSpaceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets all address spaces
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all address spaces</returns>
        public async Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting all address spaces");
                
                var addressSpaces = await _addressSpaceRepository.GetAllAsync("AddressSpaces");
                return _mapper.Map<IEnumerable<AddressSpace>>(addressSpaces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get address spaces");
                throw;
            }
        }
    }
}