
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Ipam.Core;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess.Repositories
{
    public class AddressSpaceRepository : IAddressSpaceRepository
    {
        private const string TableName = "AddressSpaces";
        private readonly TableClient _tableClient;

        public AddressSpaceRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace)
        {
            var entity = new AddressSpaceEntity
            {
                PartitionKey = "SYSTEM",
                RowKey = addressSpace.Id.ToString(),
                Name = addressSpace.Name,
                Description = addressSpace.Description,
                CreatedOn = addressSpace.CreatedOn,
                ModifiedOn = addressSpace.ModifiedOn
            };
            await _tableClient.AddEntityAsync(entity);
            return addressSpace;
        }

        public async Task DeleteAddressSpaceAsync(Guid id)
        {
            await _tableClient.DeleteEntityAsync("SYSTEM", id.ToString());
        }

        public async Task<AddressSpace> GetAddressSpaceAsync(Guid id)
        {
            var entity = await _tableClient.GetEntityAsync<AddressSpaceEntity>("SYSTEM", id.ToString());
            return new AddressSpace
            {
                Id = Guid.Parse(entity.Value.RowKey),
                Name = entity.Value.Name,
                Description = entity.Value.Description,
                CreatedOn = entity.Value.CreatedOn.Value,
                ModifiedOn = entity.Value.ModifiedOn.Value
            };
        }

        public async Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync()
        {
            var entities = _tableClient.QueryAsync<AddressSpaceEntity>();
            var addressSpaces = new List<AddressSpace>();
            await foreach (var entity in entities)
            {
                addressSpaces.Add(new AddressSpace
                {
                    Id = Guid.Parse(entity.RowKey),
                    Name = entity.Name,
                    Description = entity.Description,
                    CreatedOn = entity.CreatedOn.Value,
                    ModifiedOn = entity.ModifiedOn.Value
                });
            }
            return addressSpaces;
        }

        public async Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace)
        {
            var entity = new AddressSpaceEntity
            {
                PartitionKey = "SYSTEM",
                RowKey = addressSpace.Id.ToString(),
                Name = addressSpace.Name,
                Description = addressSpace.Description,
                ModifiedOn = addressSpace.ModifiedOn
            };
            await _tableClient.UpdateEntityAsync(entity, Azure.ETag.All, TableUpdateMode.Merge);
            return addressSpace;
        }
    }
}
