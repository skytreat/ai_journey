
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Ipam.Core;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess.Repositories
{
    public class IpAddressRepository : IIpAddressRepository
    {
        private const string TableName = "IpAddresses";
        private readonly TableClient _tableClient;

        public IpAddressRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<IpAddress> CreateIpAddressAsync(IpAddress ipAddress)
        {
            var entity = new IpAddressEntity
            {
                PartitionKey = ipAddress.AddressSpaceId.ToString(),
                RowKey = ipAddress.Id.ToString(),
                Prefix = ipAddress.Prefix,
                Tags = JsonSerializer.Serialize(ipAddress.Tags),
                ParentId = ipAddress.ParentId?.ToString(),
                CreatedOn = ipAddress.CreatedOn,
                ModifiedOn = ipAddress.ModifiedOn
            };
            await _tableClient.AddEntityAsync(entity);
            return ipAddress;
        }

        public async Task DeleteIpAddressAsync(Guid addressSpaceId, Guid id)
        {
            await _tableClient.DeleteEntityAsync(addressSpaceId.ToString(), id.ToString());
        }

        public async Task<IpAddress> GetIpAddressAsync(Guid addressSpaceId, Guid id)
        {
            var entity = await _tableClient.GetEntityAsync<IpAddressEntity>(addressSpaceId.ToString(), id.ToString());
            return new IpAddress
            {
                Id = Guid.Parse(entity.Value.RowKey),
                AddressSpaceId = Guid.Parse(entity.Value.PartitionKey),
                Prefix = entity.Value.Prefix,
                Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Value.Tags),
                ParentId = string.IsNullOrEmpty(entity.Value.ParentId) ? (Guid?)null : Guid.Parse(entity.Value.ParentId),
                CreatedOn = entity.Value.CreatedOn.Value,
                ModifiedOn = entity.Value.ModifiedOn.Value,
                ETag = entity.Value.ETag.ToString()
            };
        }

        public async Task<IEnumerable<IpAddress>> GetIpAddressesAsync(Guid addressSpaceId, string cidr, Dictionary<string, string> tags)
        {
            // This is a simplified query. A real implementation would need more complex filtering.
            var query = _tableClient.QueryAsync<IpAddressEntity>(e => e.PartitionKey == addressSpaceId.ToString());
            var ipAddresses = new List<IpAddress>();
            await foreach (var entity in query)
            {
                var ipAddress = new IpAddress
                {
                    Id = Guid.Parse(entity.RowKey),
                    AddressSpaceId = Guid.Parse(entity.PartitionKey),
                    Prefix = entity.Prefix,
                    Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Tags),
                    ParentId = string.IsNullOrEmpty(entity.ParentId) ? (Guid?)null : Guid.Parse(entity.ParentId),
                    CreatedOn = entity.CreatedOn.Value,
                    ModifiedOn = entity.ModifiedOn.Value,
                    ETag = entity.ETag.ToString()
                };

                bool match = true;
                if (!string.IsNullOrEmpty(cidr) && ipAddress.Prefix != cidr)
                {
                    match = false;
                }

                if (tags != null && tags.Any())
                {
                    foreach (var tag in tags)
                    {
                        if (!ipAddress.Tags.ContainsKey(tag.Key) || ipAddress.Tags[tag.Key] != tag.Value)
                        {
                            match = false;
                            break;
                        }
                    }
                }

                if (match)
                {
                    ipAddresses.Add(ipAddress);
                }
            }
            return ipAddresses;
        }

        public async Task<IpAddress> UpdateIpAddressAsync(IpAddress ipAddress)
        {
            var entity = new IpAddressEntity
            {
                PartitionKey = ipAddress.AddressSpaceId.ToString(),
                RowKey = ipAddress.Id.ToString(),
                Prefix = ipAddress.Prefix,
                Tags = JsonSerializer.Serialize(ipAddress.Tags),
                ParentId = ipAddress.ParentId?.ToString(),
                ModifiedOn = ipAddress.ModifiedOn
            };

            if (string.IsNullOrEmpty(ipAddress.ETag))
            {
                throw new InvalidOperationException("An ETag is required for update operations to prevent concurrency conflicts.");
            }

            await _tableClient.UpdateEntityAsync(entity, new ETag(ipAddress.ETag), TableUpdateMode.Merge);
            return ipAddress;
        }

        public async Task<IEnumerable<IpAddress>> GetChildrenAsync(Guid addressSpaceId, Guid id)
        {
            var query = _tableClient.QueryAsync<IpAddressEntity>(e => e.PartitionKey == addressSpaceId.ToString() && e.ParentId == id.ToString());
            var ipAddresses = new List<IpAddress>();
            await foreach (var entity in query)
            {
                ipAddresses.Add(new IpAddress
                {
                    Id = Guid.Parse(entity.RowKey),
                    AddressSpaceId = Guid.Parse(entity.PartitionKey),
                    Prefix = entity.Prefix,
                    Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Tags),
                    ParentId = string.IsNullOrEmpty(entity.ParentId) ? (Guid?)null : Guid.Parse(entity.ParentId),
                    CreatedOn = entity.CreatedOn.Value,
                    ModifiedOn = entity.ModifiedOn.Value,
                    ETag = entity.ETag.ToString()
                });
            }
            return ipAddresses;
        }
    }
}
