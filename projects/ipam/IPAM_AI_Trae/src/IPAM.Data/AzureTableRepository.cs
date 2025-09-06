using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using IPAM.Core;

namespace IPAM.Data
{
    public class AzureTableRepository : IRepository
    {
        private readonly TableServiceClient _tableServiceClient;
        private const string AddressSpaceTable = "AddressSpaces";
        private const string TagsTable = "Tags";
        private const string IPsTable = "IPs";

        public AzureTableRepository(string connectionString)
        {
            _tableServiceClient = new TableServiceClient(connectionString);
        }

        public async Task InitializeAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(AddressSpaceTable);
            await _tableServiceClient.CreateTableIfNotExistsAsync(TagsTable);
            await _tableServiceClient.CreateTableIfNotExistsAsync(IPsTable);
        }

        public async Task<AddressSpace> GetAddressSpaceById(Guid id)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTable);
            var response = await tableClient.GetEntityAsync<TableEntity>(id.ToString(), id.ToString());
            return MapToAddressSpace(response.Value);
        }

        public async Task<List<AddressSpace>> GetAddressSpaces(string keyword = null, DateTime? createdAfter = null, DateTime? createdBefore = null)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTable);
            var query = $"PartitionKey eq RowKey";
            if (!string.IsNullOrEmpty(keyword))
            {
                query += $" and (Name eq '{keyword}' or Description eq '{keyword}')";
            }
            if (createdAfter.HasValue)
            {
                query += $" and CreatedOn ge datetime'{createdAfter.Value:o}'";
            }
            if (createdBefore.HasValue)
            {
                query += $" and CreatedOn le datetime'{createdBefore.Value:o}'";
            }

            var entities = tableClient.QueryAsync<TableEntity>(query);
            var addressSpaces = new List<AddressSpace>();
            await foreach (var entity in entities)
            {
                addressSpaces.Add(MapToAddressSpace(entity));
            }
            return addressSpaces;
        }

        public async Task AddAddressSpace(AddressSpace addressSpace)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTable);
            var entity = new TableEntity(addressSpace.Id.ToString(), addressSpace.Id.ToString())
            {
                {"Name", addressSpace.Name},
                {"Description", addressSpace.Description},
                {"CreatedOn", addressSpace.CreatedOn},
                {"ModifiedOn", addressSpace.ModifiedOn}
            };
            await tableClient.AddEntityAsync(entity);
        }

        public async Task UpdateAddressSpace(AddressSpace addressSpace)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTable);
            var entity = new TableEntity(addressSpace.Id.ToString(), addressSpace.Id.ToString())
            {
                {"Name", addressSpace.Name},
                {"Description", addressSpace.Description},
                {"CreatedOn", addressSpace.CreatedOn},
                {"ModifiedOn", addressSpace.ModifiedOn}
            };
            await tableClient.UpdateEntityAsync(entity, ETag.All);
        }

        public async Task DeleteAddressSpace(Guid id)
        {
            var tableClient = _tableServiceClient.GetTableClient(AddressSpaceTable);
            await tableClient.DeleteEntityAsync(id.ToString(), id.ToString());
        }

        private AddressSpace MapToAddressSpace(TableEntity entity)
        {
            return new AddressSpace
            {
                Id = Guid.Parse(entity.RowKey),
                Name = entity["Name"].ToString(),
                Description = entity["Description"].ToString(),
                CreatedOn = DateTime.Parse(entity["CreatedOn"].ToString()),
                ModifiedOn = DateTime.Parse(entity["ModifiedOn"].ToString())
            };
        }

        public async Task<IP> GetIpById(Guid addressSpaceId, Guid id)
{
    var tableClient = _tableServiceClient.GetTableClient(IPsTable);
    var entity = await tableClient.GetEntityAsync<TableEntity>(addressSpaceId.ToString(), id.ToString());
    return MapToIP(entity.Value);
}

public async Task<IP> GetIpByPrefix(Guid addressSpaceId, string prefix)
{
    var tableClient = _tableServiceClient.GetTableClient(IPsTable);
    var query = tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == addressSpaceId.ToString() && e.RowKey.Contains(prefix));
    await foreach (var entity in query)
    {
        return MapToIP(entity);
    }
    return null;
}

public async Task<List<IP>> GetIpsByTags(Guid addressSpaceId, Dictionary<string, string> tags)
{
    var tableClient = _tableServiceClient.GetTableClient(IPsTable);
    var results = new List<IP>();
    
    // 这里需要根据实际需求实现标签过滤逻辑
    var query = tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == addressSpaceId.ToString());
    await foreach (var entity in query)
    {
        results.Add(MapToIP(entity));
    }
    return results;
}

public async Task<List<IP>> GetChildIps(Guid addressSpaceId, Guid parentId)
{
    var tableClient = _tableServiceClient.GetTableClient(IPsTable);
    var results = new List<IP>();
    var query = tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == addressSpaceId.ToString() && e["ParentId"] == parentId.ToString());
    await foreach (var entity in query)
    {
        results.Add(MapToIP(entity));
    }
    return results;
}

public async Task AddIp(IP ip)
{
    var tableClient = _tableServiceClient.GetTableClient(IPsTable);
    var entity = MapToTableEntity(ip);
    await tableClient.AddEntityAsync(entity);
}

public async Task UpdateIp(IP ip)
{
    var tableClient = _tableServiceClient.GetTableClient(IPsTable);
    var entity = MapToTableEntity(ip);
    await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
}

public async Task DeleteIp(Guid addressSpaceId, Guid id)
{
    var tableClient = _tableServiceClient.GetTableClient(IPsTable);
    await tableClient.DeleteEntityAsync(addressSpaceId.ToString(), id.ToString());
}

public async Task<Tag> GetTagById(Guid addressSpaceId, string name)
{
    var tableClient = _tableServiceClient.GetTableClient(TagsTable);
    var response = await tableClient.GetEntityAsync<TableEntity>(addressSpaceId.ToString(), name);
    return MapToTag(response.Value);
}

public async Task<List<Tag>> GetTags(Guid addressSpaceId, string keyword = null)
{
    var tableClient = _tableServiceClient.GetTableClient(TagsTable);
    var query = tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == addressSpaceId.ToString());
    var tags = new List<Tag>();
    await foreach (var entity in query)
    {
        tags.Add(MapToTag(entity));
    }
    return tags;
}

public async Task AddTag(Tag tag)
{
    var tableClient = _tableServiceClient.GetTableClient(TagsTable);
    var entity = MapToTableEntity(tag);
    await tableClient.AddEntityAsync(entity);
}

public async Task UpdateTag(Tag tag)
{
    var tableClient = _tableServiceClient.GetTableClient(TagsTable);
    var entity = MapToTableEntity(tag);
    await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
}

public async Task DeleteTag(Guid addressSpaceId, string name)
{
    var tableClient = _tableServiceClient.GetTableClient(TagsTable);
    await tableClient.DeleteEntityAsync(addressSpaceId.ToString(), name);
}

private IP MapToIP(TableEntity entity)
{
    return new IP
    {
        AddressSpaceId = Guid.Parse(entity.PartitionKey),
        Id = Guid.Parse(entity.RowKey),
        Prefix = entity["Prefix"]?.ToString(),
        Tags = ParseTags(entity["Tags"]?.ToString()),
        CreatedOn = DateTime.Parse(entity["CreatedOn"]?.ToString()),
        ModifiedOn = DateTime.Parse(entity["ModifiedOn"]?.ToString()),
        ParentId = string.IsNullOrEmpty(entity["ParentId"]?.ToString()) ? null : Guid.Parse(entity["ParentId"]?.ToString()),
        ChildrenIds = new List<Guid>()
    };
}

private Dictionary<string, string> ParseTags(string tagsString)
{
    if (string.IsNullOrEmpty(tagsString))
        return new Dictionary<string, string>();
        
    var tags = new Dictionary<string, string>();
    var pairs = tagsString.Split(',');
    foreach (var pair in pairs)
    {
        var keyValue = pair.Split('=');
        if (keyValue.Length == 2)
            tags[keyValue[0]] = keyValue[1];
    }
    return tags;
}

private TableEntity MapToTableEntity(IP ip)
{
    return new TableEntity(ip.AddressSpaceId.ToString(), ip.Id.ToString())
    {
        ["Prefix"] = ip.Prefix,
        ["Tags"] = string.Join(",", ip.Tags.Select(t => $"{t.Key}={t.Value}")),
        ["CreatedOn"] = ip.CreatedOn,
        ["ModifiedOn"] = ip.ModifiedOn,
        ["ParentId"] = ip.ParentId?.ToString()
    };
}

private Tag MapToTag(TableEntity entity)
{
    return new Tag
    {
        AddressSpaceId = Guid.Parse(entity.PartitionKey),
        Name = entity.RowKey,
        Description = entity["Description"]?.ToString(),
        CreatedOn = DateTime.Parse(entity["CreatedOn"]?.ToString()),
        ModifiedOn = DateTime.Parse(entity["ModifiedOn"]?.ToString()),
        Type = (TagType)Enum.Parse(typeof(TagType), entity["Type"]?.ToString())
    };
}

private TableEntity MapToTableEntity(Tag tag)
{
    return new TableEntity(tag.AddressSpaceId.ToString(), tag.Name)
    {
        ["Description"] = tag.Description,
        ["CreatedOn"] = tag.CreatedOn,
        ["ModifiedOn"] = tag.ModifiedOn,
        ["Type"] = tag.Type.ToString()
    };
}
    }
}