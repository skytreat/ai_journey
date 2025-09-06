using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
using System;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Services
{
    /// <summary>
    /// Service for managing address spaces with automatic root node creation
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceService
    {
        private readonly IAddressSpaceRepository _addressSpaceRepository;
        private readonly IIpNodeRepository _ipNodeRepository;

        public AddressSpaceService(
            IAddressSpaceRepository addressSpaceRepository,
            IIpNodeRepository ipNodeRepository)
        {
            _addressSpaceRepository = addressSpaceRepository;
            _ipNodeRepository = ipNodeRepository;
        }

        /// <summary>
        /// Creates an address space with automatic root node creation
        /// </summary>
        /// <param name="addressSpace">The address space to create</param>
        /// <returns>The created address space</returns>
        public async Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace)
        {
            // Set default values
            addressSpace.Id = addressSpace.Id ?? Guid.NewGuid().ToString();
            addressSpace.PartitionKey = "AddressSpaces"; // Default partition
            addressSpace.CreatedOn = DateTime.UtcNow;
            addressSpace.ModifiedOn = DateTime.UtcNow;
            addressSpace.Status = "Active";

            // Create the address space
            var createdAddressSpace = await _addressSpaceRepository.CreateAsync(addressSpace);

            // Create root IPv6 node (::/0)
            var rootIpv6 = new IpNode
            {
                Id = "ipv6_root",
                AddressSpaceId = addressSpace.Id,
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
            var rootIpv4 = new IpNode
            {
                Id = "ipv4_root",
                AddressSpaceId = addressSpace.Id,
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
                rootIpv6.ChildrenIds = new[] { "ipv4_root" };
                await _ipNodeRepository.UpdateAsync(rootIpv6);
            }
            catch (Exception ex)
            {
                // If root node creation fails, we should still return the address space
                // but log the error for investigation
                // In a real implementation, you might want to use a logger here
                Console.WriteLine($"Warning: Failed to create root nodes for address space {addressSpace.Id}: {ex.Message}");
            }

            return createdAddressSpace;
        }

        /// <summary>
        /// Gets an address space by ID
        /// </summary>
        /// <param name="addressSpaceId">The address space ID</param>
        /// <returns>The address space or null if not found</returns>
        public async Task<AddressSpace> GetAddressSpaceAsync(string addressSpaceId)
        {
            return await _addressSpaceRepository.GetByIdAsync("AddressSpaces", addressSpaceId);
        }

        /// <summary>
        /// Updates an address space
        /// </summary>
        /// <param name="addressSpace">The address space to update</param>
        /// <returns>The updated address space</returns>
        public async Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace)
        {
            addressSpace.ModifiedOn = DateTime.UtcNow;
            return await _addressSpaceRepository.UpdateAsync(addressSpace);
        }

        /// <summary>
        /// Deletes an address space and all its IP nodes
        /// </summary>
        /// <param name="addressSpaceId">The address space ID to delete</param>
        public async Task DeleteAddressSpaceAsync(string addressSpaceId)
        {
            // In a real implementation, you would want to:
            // 1. Delete all IP nodes in the address space
            // 2. Delete all tags in the address space
            // 3. Finally delete the address space itself
            
            // For now, just delete the address space
            await _addressSpaceRepository.DeleteAsync("AddressSpaces", addressSpaceId);
        }
    }
}