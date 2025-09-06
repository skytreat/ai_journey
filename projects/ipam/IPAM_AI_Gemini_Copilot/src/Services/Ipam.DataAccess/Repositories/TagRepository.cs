
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Ipam.Core;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess.Repositories
{
    public class TagRepository : ITagRepository
    {
        private const string TableName = "TagDefinitions";
        private readonly TableClient _tableClient;

        public TagRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<Tag> CreateTagAsync(Tag tag)
        {
            var entity = new TagDefinitionEntity
            {
                PartitionKey = tag.AddressSpaceId.ToString(),
                RowKey = tag.Name,
                Description = tag.Description,
                Type = tag.Type.ToString(),
                KnownValues = JsonSerializer.Serialize(tag.KnownValues),
                Attributes = JsonSerializer.Serialize(tag.Attributes),
                CreatedOn = tag.CreatedOn,
                ModifiedOn = tag.ModifiedOn
            };
            await _tableClient.AddEntityAsync(entity);
            return tag;
        }

        public async Task DeleteTagAsync(Guid addressSpaceId, string name)
        {
            await _tableClient.DeleteEntityAsync(addressSpaceId.ToString(), name);
        }

        public async Task<Tag> GetTagAsync(Guid addressSpaceId, string name)
        {
            var entity = await _tableClient.GetEntityAsync<TagDefinitionEntity>(addressSpaceId.ToString(), name);
            return new Tag
            {
                AddressSpaceId = Guid.Parse(entity.Value.PartitionKey),
                Name = entity.Value.RowKey,
                Description = entity.Value.Description,
                Type = Enum.Parse<TagType>(entity.Value.Type),
                KnownValues = JsonSerializer.Deserialize<List<string>>(entity.Value.KnownValues),
                Attributes = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(entity.Value.Attributes),
                CreatedOn = entity.Value.CreatedOn.Value,
                ModifiedOn = entity.Value.ModifiedOn.Value
            };
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync(Guid addressSpaceId)
        {
            var entities = _tableClient.QueryAsync<TagDefinitionEntity>(e => e.PartitionKey == addressSpaceId.ToString());
            var tags = new List<Tag>();
            await foreach (var entity in entities)
            {
                tags.Add(new Tag
                {
                    AddressSpaceId = Guid.Parse(entity.PartitionKey),
                    Name = entity.RowKey,
                    Description = entity.Description,
                    Type = Enum.Parse<TagType>(entity.Type),
                    KnownValues = JsonSerializer.Deserialize<List<string>>(entity.KnownValues),
                    Attributes = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(entity.Attributes),
                    CreatedOn = entity.CreatedOn.Value,
                    ModifiedOn = entity.ModifiedOn.Value
                });
            }
            return tags;
        }

        public async Task<Tag> UpdateTagAsync(Tag tag)
        {
            var entity = new TagDefinitionEntity
            {
                PartitionKey = tag.AddressSpaceId.ToString(),
                RowKey = tag.Name,
                Description = tag.Description,
                Type = tag.Type.ToString(),
                KnownValues = JsonSerializer.Serialize(tag.KnownValues),
                Attributes = JsonSerializer.Serialize(tag.Attributes),
                ModifiedOn = tag.ModifiedOn
            };
            await _tableClient.UpdateEntityAsync(entity, Azure.ETag.All, TableUpdateMode.Merge);
            return tag;
        }
    }
}
