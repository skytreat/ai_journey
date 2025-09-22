using Azure.Data.Tables;
using Ipam.DataAccess.Extensions;
using Ipam.DataAccess.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Ipam.DataAccess.Entities;

namespace Ipam.DataAccess.Repositories
{
    /// <summary>
    /// Implementation of address space repository using Azure Table Storage
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceRepository : BaseRepository<AddressSpaceEntity>, IAddressSpaceRepository
    {
        private const string TableName = "AddressSpaces";

        public AddressSpaceRepository(IConfiguration configuration)
            : base(configuration, TableName)
        {
        }

        public async Task<AddressSpaceEntity> GetByIdAsync(string partitionId, string addressSpaceId)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
                await TableClient.GetEntityAsync<AddressSpaceEntity>(partitionId, addressSpaceId));
        }

        public async Task<IEnumerable<AddressSpaceEntity>> GetAllAsync(string partitionId)
        {
            var query = TableClient.QueryAsync<AddressSpaceEntity>(filter: $"PartitionKey eq '{partitionId}'");
            var results = new List<AddressSpaceEntity>();

            await foreach (var addressSpace in query)
            {
                results.Add(addressSpace);
            }

            return results;
        }

        public async Task<IEnumerable<AddressSpaceEntity>> QueryAsync(string nameFilter = null, DateTime? createdAfter = null)
        {
            var query = TableClient.QueryAsync<AddressSpaceEntity>();
            var results = new List<AddressSpaceEntity>();

            await foreach (var addressSpace in query)
            {
                if (nameFilter != null && !addressSpace.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                if (createdAfter.HasValue && addressSpace.CreatedOn <= createdAfter.Value)
                    continue;

                results.Add(addressSpace);
            }

            return results;
        }

        public async Task<AddressSpaceEntity> CreateAsync(AddressSpaceEntity addressSpace)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
            {
                addressSpace.CreatedOn = DateTime.UtcNow;
                addressSpace.ModifiedOn = DateTime.UtcNow;
                await TableClient.AddEntityAsync(addressSpace);
                return addressSpace;
            });
        }

        public async Task<AddressSpaceEntity> UpdateAsync(AddressSpaceEntity addressSpace)
        {
            addressSpace.ModifiedOn = DateTime.UtcNow;
            await TableClient.UpdateEntityAsync(addressSpace, addressSpace.ETag);
            return addressSpace;
        }

        public async Task DeleteAsync(string partitionId, string addressSpaceId)
        {
            await TableClient.DeleteEntityAsync(partitionId, addressSpaceId);
        }
    }
}