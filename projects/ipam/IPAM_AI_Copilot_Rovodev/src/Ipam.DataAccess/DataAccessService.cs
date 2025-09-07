using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Azure;
using Azure.Data.Tables;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess
{
    /// <summary>
    /// Simple implementation of data access service using repositories
    /// </summary>
    public class DataAccessService : IDataAccessService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DataAccessService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // IP Address management implementation
        public async Task<IpAllocation> CreateIPAddressAsync(IpAllocation ipAllocation)
        {
            // Convert IpAllocation to IpNode for storage
            var ipNode = new IpNode
            {
                PartitionKey = ipAllocation.AddressSpaceId,
                RowKey = ipAllocation.Id,
                Prefix = ipAllocation.Prefix,
                ParentId = ipAllocation.ParentId,
                Tags = ipAllocation.Tags.ToDictionary(t => t.Name, t => t.Value),
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };

            await _unitOfWork.IpNodes.CreateAsync(ipNode);
            await _unitOfWork.SaveChangesAsync();
            return ipAllocation;
        }

        public async Task<IpAllocation> GetIPAddressAsync(string addressSpaceId, string ipId)
        {
            var ipNode = await _unitOfWork.IpNodes.GetByIdAsync(addressSpaceId, ipId);
            if (ipNode == null) return null;

            return new IpAllocation
            {
                Id = ipNode.RowKey,
                AddressSpaceId = ipNode.PartitionKey,
                Prefix = ipNode.Prefix,
                ParentId = ipNode.ParentId,
                Tags = ipNode.Tags.Select(t => new IpAllocationTag { Name = t.Key, Value = t.Value }).ToList(),
                CreatedOn = ipNode.CreatedOn,
                ModifiedOn = ipNode.ModifiedOn
            };
        }

        public async Task<IEnumerable<IpAllocation>> GetIPAddressesAsync(string addressSpaceId, string cidr = null, Dictionary<string, string> tags = null)
        {
            IEnumerable<IpNode> ipNodes;

            if (!string.IsNullOrEmpty(cidr))
            {
                ipNodes = await _unitOfWork.IpNodes.GetByPrefixAsync(addressSpaceId, cidr);
            }
            else if (tags != null && tags.Any())
            {
                ipNodes = await _unitOfWork.IpNodes.GetByTagsAsync(addressSpaceId, tags);
            }
            else
            {
                // Get all nodes for the address space
                ipNodes = await _unitOfWork.IpNodes.GetChildrenAsync(addressSpaceId, null);
            }

            return ipNodes.Select(node => new IpAllocation
            {
                Id = node.RowKey,
                AddressSpaceId = node.PartitionKey,
                Prefix = node.Prefix,
                ParentId = node.ParentId,
                Tags = node.Tags.Select(t => new IpAllocationTag { Name = t.Key, Value = t.Value }).ToList(),
                CreatedOn = node.CreatedOn,
                ModifiedOn = node.ModifiedOn
            });
        }

        public async Task<IpAllocation> UpdateIPAddressAsync(IpAllocation ipAllocation)
        {
            var ipNode = await _unitOfWork.IpNodes.GetByIdAsync(ipAllocation.AddressSpaceId, ipAllocation.Id);
            if (ipNode == null) return null;

            ipNode.Prefix = ipAllocation.Prefix;
            ipNode.ParentId = ipAllocation.ParentId;
            ipNode.Tags = ipAllocation.Tags.ToDictionary(t => t.Name, t => t.Value);
            ipNode.ModifiedOn = DateTime.UtcNow;

            await _unitOfWork.IpNodes.UpdateAsync(ipNode);
            await _unitOfWork.SaveChangesAsync();
            return ipAllocation;
        }

        public async Task DeleteIPAddressAsync(string addressSpaceId, string ipId)
        {
            await _unitOfWork.IpNodes.DeleteAsync(addressSpaceId, ipId);
            await _unitOfWork.SaveChangesAsync();
        }

        // Address Space management implementation
        public async Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace)
        {
            addressSpace.PartitionKey = "AddressSpaces";
            addressSpace.RowKey = Guid.NewGuid().ToString();
            addressSpace.CreatedOn = DateTime.UtcNow;
            addressSpace.ModifiedOn = DateTime.UtcNow;

            await _unitOfWork.AddressSpaces.CreateAsync(addressSpace);
            await _unitOfWork.SaveChangesAsync();
            return addressSpace;
        }

        public async Task<AddressSpace> GetAddressSpaceAsync(string addressSpaceId)
        {
            return await _unitOfWork.AddressSpaces.GetByIdAsync("AddressSpaces", addressSpaceId);
        }

        public async Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync()
        {
            return await _unitOfWork.AddressSpaces.QueryAsync();
        }

        public async Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace)
        {
            addressSpace.ModifiedOn = DateTime.UtcNow;
            await _unitOfWork.AddressSpaces.UpdateAsync(addressSpace);
            await _unitOfWork.SaveChangesAsync();
            return addressSpace;
        }

        public async Task DeleteAddressSpaceAsync(string addressSpaceId)
        {
            await _unitOfWork.AddressSpaces.DeleteAsync("AddressSpaces", addressSpaceId);
            await _unitOfWork.SaveChangesAsync();
        }

        // Tag management implementation
        public async Task<Tag> CreateTagAsync(string addressSpaceId, Tag tag)
        {
            tag.PartitionKey = addressSpaceId;
            tag.RowKey = Guid.NewGuid().ToString();
            tag.CreatedOn = DateTime.UtcNow;
            tag.ModifiedOn = DateTime.UtcNow;

            await _unitOfWork.Tags.CreateAsync(tag);
            await _unitOfWork.SaveChangesAsync();
            return tag;
        }

        public async Task<Tag> GetTagAsync(string addressSpaceId, string tagName)
        {
            return await _unitOfWork.Tags.GetByNameAsync(addressSpaceId, tagName);
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync(string addressSpaceId)
        {
            return await _unitOfWork.Tags.GetAllAsync(addressSpaceId);
        }

        public async Task<Tag> UpdateTagAsync(string addressSpaceId, Tag tag)
        {
            tag.ModifiedOn = DateTime.UtcNow;
            await _unitOfWork.Tags.UpdateAsync(tag);
            await _unitOfWork.SaveChangesAsync();
            return tag;
        }

        public async Task DeleteTagAsync(string addressSpaceId, string tagName)
        {
            await _unitOfWork.Tags.DeleteAsync(addressSpaceId, tagName);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}