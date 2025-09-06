
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
        private const string TagDefinitionsTableName = "TagDefinitions";
        private const string TagImplicationsTableName = "TagImplications";
        private readonly TableClient _tagDefinitionsTableClient;
        private readonly TableClient _tagImplicationsTableClient;

        public TagRepository(TableServiceClient tableServiceClient)
        {
            _tagDefinitionsTableClient = tableServiceClient.GetTableClient(TagDefinitionsTableName);
            _tagDefinitionsTableClient.CreateIfNotExists();
            _tagImplicationsTableClient = tableServiceClient.GetTableClient(TagImplicationsTableName);
            _tagImplicationsTableClient.CreateIfNotExists();
        }

        public async Task<Tag> CreateTagAsync(Tag tag)
        {
            var entity = new TagEntity
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
            await _tagDefinitionsTableClient.AddEntityAsync(entity);
            return tag;
        }

        public async Task DeleteTagAsync(Guid addressSpaceId, string name)
        {
            await _tagDefinitionsTableClient.DeleteEntityAsync(addressSpaceId.ToString(), name);
        }

        public async Task<Tag> GetTagAsync(Guid addressSpaceId, string name)
        {
            var entity = await _tagDefinitionsTableClient.GetEntityAsync<TagEntity>(addressSpaceId.ToString(), name);
            return new Tag
            {
                AddressSpaceId = Guid.Parse(entity.Value.PartitionKey),
                Name = entity.Value.RowKey,
                Description = entity.Value.Description,
                Type = Enum.Parse<TagType>(entity.Value.Type),
                KnownValues = JsonSerializer.Deserialize<List<string>>(entity.Value.KnownValues),
                Attributes = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(entity.Value.Attributes),
                CreatedOn = entity.Value.CreatedOn.Value,
                ModifiedOn = entity.Value.ModifiedOn.Value,
                ETag = entity.Value.ETag.ToString()
            };
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync(Guid addressSpaceId)
        {
            var entities = _tagDefinitionsTableClient.QueryAsync<TagEntity>(e => e.PartitionKey == addressSpaceId.ToString());
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
                    ModifiedOn = entity.ModifiedOn.Value,
                    ETag = entity.ETag.ToString()
                });
            }
            return tags;
        }

        public async Task<Tag> UpdateTagAsync(Tag tag)
        {
            var entity = new TagEntity
            {
                PartitionKey = tag.AddressSpaceId.ToString(),
                RowKey = tag.Name,
                Description = tag.Description,
                Type = tag.Type.ToString(),
                KnownValues = JsonSerializer.Serialize(tag.KnownValues),
                Attributes = JsonSerializer.Serialize(tag.Attributes),
                ModifiedOn = tag.ModifiedOn
            };
            if (string.IsNullOrEmpty(tag.ETag))
            {
                throw new InvalidOperationException("An ETag is required for update operations to prevent concurrency conflicts.");
            }
            await _tagDefinitionsTableClient.UpdateEntityAsync(entity, new Azure.ETag(tag.ETag), TableUpdateMode.Merge);
            return tag;
        }

        public async Task<TagImplication> CreateTagImplicationAsync(TagImplication tagImplication)
        {
            var entity = new TagImplicationEntity
            {
                PartitionKey = tagImplication.AddressSpaceId.ToString(),
                RowKey = tagImplication.IfTagValue,
                ThenTagValue = tagImplication.ThenTagValue
            };
            await _tagImplicationsTableClient.AddEntityAsync(entity);
            return tagImplication;
        }

        public async Task DeleteTagImplicationAsync(Guid addressSpaceId, string ifTagValue)
        {
            await _tagImplicationsTableClient.DeleteEntityAsync(addressSpaceId.ToString(), ifTagValue);
        }

        public async Task<TagImplication> GetTagImplicationAsync(Guid addressSpaceId, string ifTagValue)
        {
            var entity = await _tagImplicationsTableClient.GetEntityAsync<TagImplicationEntity>(addressSpaceId.ToString(), ifTagValue);
            return new TagImplication
            {
                AddressSpaceId = Guid.Parse(entity.Value.PartitionKey),
                IfTagValue = entity.Value.RowKey,
                ThenTagValue = entity.Value.ThenTagValue
            };
        }

        public async Task<IEnumerable<TagImplication>> GetTagImplicationsAsync(Guid addressSpaceId)
        {
            var entities = _tagImplicationsTableClient.QueryAsync<TagImplicationEntity>(e => e.PartitionKey == addressSpaceId.ToString());
            var tagImplications = new List<TagImplication>();
            await foreach (var entity in entities)
            {
                tagImplications.Add(new TagImplication
                {
                    AddressSpaceId = Guid.Parse(entity.PartitionKey),
                    IfTagValue = entity.RowKey,
                    ThenTagValue = entity.ThenTagValue
                });
            }
            return tagImplications;
        }
    }
}
