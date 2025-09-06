using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Azure.Data.Tables;
using Ipam.DataAccess.Models;

namespace Ipam.DataAccess
{
    // Data Access Service Implementation
    public class DataAccessService : IDataAccessService
    {
        private readonly TableServiceClient _tableServiceClient;
        private const string IpTableName = "IPAddresses";
        private const string AddressSpaceTableName = "AddressSpaces";
        private const string TagTableName = "Tags";

        public DataAccessService(TableServiceClient tableServiceClient)
        {
            _tableServiceClient = tableServiceClient;
            // Ensure tables exist
            _tableServiceClient.CreateTableIfNotExistsAsync(IpTableName).Wait();
            _tableServiceClient.CreateTableIfNotExistsAsync(AddressSpaceTableName).Wait();
            _tableServiceClient.CreateTableIfNotExistsAsync(TagTableName).Wait();
        }

        // IP Address management implementation
        public async Task<IPAddress> CreateIPAddressAsync(IPAddress ipAddress)
        {
            var tableClient = _tableServiceClient.GetTableClient(IpTableName);
            var entity = new TableEntity(ipAddress.AddressSpaceId, ipAddress.Id)
            {
                ["Prefix"] = ipAddress.Prefix,
                ["ParentId"] = ipAddress.ParentId,
                ["Tags"] = string.Join(";", ipAddress.Tags.Select(t => $"{t.Name}={t.Value}"))
            };
            await tableClient.AddEntityAsync(entity);
            return ipAddress;
        }

        public async Task<IPAddress> GetIPAddressAsync(string addressSpaceId, string ipId)
        {
            var tableClient = _tableServiceClient.GetTableClient(IpTableName);
            try
            {
                var entity = await tableClient.GetEntityAsync<TableEntity>(addressSpaceId, ipId);
                var ipAddress = new IPAddress
                {
                    Id = entity.Value.RowKey,
                    AddressSpaceId = entity.Value.PartitionKey,
                    Prefix = entity.Value["Prefix"].ToString(),
                    ParentId = entity.Value.ContainsKey("ParentId") ? entity.Value["ParentId"].ToString() : null
                };
                
                // Parse tags
                if (entity.Value.ContainsKey("Tags"))
                {
                    var tagsString = entity.Value["Tags"].ToString();
                    if (!string.IsNullOrEmpty(tagsString))
                    {
                        var tagPairs = tagsString.Split(';');
                        foreach (var tagPair in tagPairs)
                        {
                            var parts = tagPair.Split('=');
                            if (parts.Length == 2)
                            {
                                ipAddress.Tags.Add(new Tag { Name = parts[0], Value = parts[1] });
                            }
                        }
                    }
                }
                
                return ipAddress;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<IPAddress>> GetIPAddressesAsync(string addressSpaceId, string cidr = null, Dictionary<string, string> tags = null)
        {
            var tableClient = _tableServiceClient.GetTableClient(IpTableName);
            var ipAddresses = new List<IPAddress>();
            
            // Query by address space
            var query = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{addressSpaceId}'");
            
            await foreach (var entity in query)
            {
                var ipAddress = new IPAddress
                {
                    Id = entity.RowKey,
                    AddressSpaceId = entity.PartitionKey,
                    Prefix = entity["Prefix"].ToString(),
                    ParentId = entity.ContainsKey("ParentId") ? entity["ParentId"].ToString() : null
                };
                
                // Parse tags
                if (entity.ContainsKey("Tags"))
                {
                    var tagsString = entity["Tags"].ToString();
                    if (!string.IsNullOrEmpty(tagsString))
                    {
                        var tagPairs = tagsString.Split(';');
                        foreach (var tagPair in tagPairs)
                        {
                            var parts = tagPair.Split('=');
                            if (parts.Length == 2)
                            {
                                ipAddress.Tags.Add(new Tag { Name = parts[0], Value = parts[1] });
                            }
                        }
                    }
                }
                
                // Apply CIDR filter if provided
                if (string.IsNullOrEmpty(cidr) || ipAddress.Prefix.Contains(cidr))
                {
                    // Apply tags filter if provided
                    if (tags == null || tags.Count == 0)
                    {
                        ipAddresses.Add(ipAddress);
                    }
                    else
                    {
                        var match = true;
                        foreach (var tag in tags)
                        {
                            var existingTag = ipAddress.Tags.FirstOrDefault(t => t.Name == tag.Key);
                            if (existingTag == null || existingTag.Value != tag.Value)
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            ipAddresses.Add(ipAddress);
                        }
                    }
                }
            }
            
            return ipAddresses;
        }

        public async Task<IPAddress> UpdateIPAddressAsync(IPAddress ipAddress)
        {
            var tableClient = _tableServiceClient.GetTableClient(IpTableName);
            var entity = new TableEntity(ipAddress.AddressSpaceId, ipAddress.Id)
            {
                ["Prefix"] = ipAddress.Prefix,
                ["ParentId"] = ipAddress.ParentId,
                ["Tags"] = string.Join(";", ipAddress.Tags.Select(t => $"{t.Name}={t.Value}"))
            };
            await tableClient.UpdateEntityAsync(entity, ETag.All);
            return ipAddress;
        }

        public async Task DeleteIPAddressAsync(string addressSpaceId, string ipId)
        {
            var tableClient = _tableServiceClient.GetTableClient(IpTableName);
            await tableClient.DeleteEntityAsync(addressSpaceId, ipId);
        }

        // Address Space management implementation
        public async Task<AddressSpace> CreateAddressSpaceAsync(AddressSpace addressSpace)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTableName);
            var entity = new TableEntity(addressSpace.Id, addressSpace.Id)
            {
                ["Name"] = addressSpace.Name,
                ["Description"] = addressSpace.Description,
                ["PartitionKey"] = addressSpace.PartitionKey
            };
            await tableClient.AddEntityAsync(entity);
            return addressSpace;
        }

        public async Task<AddressSpace> GetAddressSpaceAsync(string addressSpaceId)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTableName);
            try
            {
                var entity = await tableClient.GetEntityAsync<TableEntity>(addressSpaceId, addressSpaceId);
                return new AddressSpace
                {
                    Id = entity.Value.RowKey,
                    Name = entity.Value["Name"].ToString(),
                    Description = entity.Value["Description"].ToString(),
                    PartitionKey = entity.Value["PartitionKey"].ToString()
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<AddressSpace>> GetAddressSpacesAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTableName);
            var addressSpaces = new List<AddressSpace>();
            
            var query = tableClient.QueryAsync<TableEntity>();
            
            await foreach (var entity in query)
            {
                addressSpaces.Add(new AddressSpace
                {
                    Id = entity.RowKey,
                    Name = entity["Name"].ToString(),
                    Description = entity["Description"].ToString(),
                    PartitionKey = entity["PartitionKey"].ToString()
                });
            }
            
            return addressSpaces;
        }

        public async Task<AddressSpace> UpdateAddressSpaceAsync(AddressSpace addressSpace)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTableName);
            var entity = new TableEntity(addressSpace.Id, addressSpace.Id)
            {
                ["Name"] = addressSpace.Name,
                ["Description"] = addressSpace.Description,
                ["PartitionKey"] = addressSpace.PartitionKey
            };
            await tableClient.UpdateEntityAsync(entity, ETag.All);
            return addressSpace;
        }

        public async Task DeleteAddressSpaceAsync(string addressSpaceId)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTableName);
            await tableClient.DeleteEntityAsync(addressSpaceId, addressSpaceId);
        }

        // Tag management implementation
        public async Task<Tag> CreateTagAsync(string addressSpaceId, Tag tag)
        {
            var tableClient = _tableServiceClient.GetTableClient(TagTableName);
            var entity = new TableEntity(addressSpaceId, tag.Name)
            {
                ["Description"] = tag.Description,
                ["Type"] = tag.Type.ToString(),
                ["KnownValues"] = string.Join(";", tag.KnownValues),
                ["Value"] = tag.Value
            };
            await tableClient.AddEntityAsync(entity);
            return tag;
        }

        public async Task<Tag> GetTagAsync(string addressSpaceId, string tagName)
        {
            var tableClient = _tableServiceClient.GetTableClient(TagTableName);
            try
            {
                var entity = await tableClient.GetEntityAsync<TableEntity>(addressSpaceId, tagName);
                return new Tag
                {
                    Name = entity.Value.RowKey,
                    Description = entity.Value["Description"].ToString(),
                    Type = Enum.Parse<TagType>(entity.Value["Type"].ToString()),
                    Value = entity.Value["Value"].ToString(),
                    KnownValues = entity.Value["KnownValues"].ToString().Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList()
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync(string addressSpaceId)
        {
            var tableClient = _tableServiceClient.GetTableClient(TagTableName);
            var tags = new List<Tag>();
            
            var query = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{addressSpaceId}'");
            
            await foreach (var entity in query)
            {
                tags.Add(new Tag
                {
                    Name = entity.RowKey,
                    Description = entity["Description"].ToString(),
                    Type = Enum.Parse<TagType>(entity["Type"].ToString()),
                    Value = entity["Value"].ToString(),
                    KnownValues = entity["KnownValues"].ToString().Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList()
                });
            }
            
            return tags;
        }

        public async Task<Tag> UpdateTagAsync(string addressSpaceId, Tag tag)
        {
            var tableClient = _tableServiceClient.GetTableClient(TagTableName);
            var entity = new TableEntity(addressSpaceId, tag.Name)
            {
                ["Description"] = tag.Description,
                ["Type"] = tag.Type.ToString(),
                ["KnownValues"] = string.Join(";", tag.KnownValues),
                ["Value"] = tag.Value
            };
            await tableClient.UpdateEntityAsync(entity, ETag.All);
            return tag;
        }

        public async Task DeleteTagAsync(string addressSpaceId, string tagName)
        {
            var tableClient = _tableServiceClient.GetTableClient(TagTableName);
            await tableClient.DeleteEntityAsync(addressSpaceId, tagName);
        }
    }
}