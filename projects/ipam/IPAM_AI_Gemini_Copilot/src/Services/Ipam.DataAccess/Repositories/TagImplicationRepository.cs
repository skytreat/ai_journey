
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Ipam.Core;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess.Repositories
{
    public class TagImplicationRepository : ITagImplicationRepository
    {
        private const string TableName = "TagImplications";
        private readonly TableClient _tableClient;

        public TagImplicationRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<TagImplication> CreateTagImplicationAsync(TagImplication tagImplication)
        {
            var entity = new TagImplicationEntity
            {
                PartitionKey = tagImplication.AddressSpaceId.ToString(),
                RowKey = tagImplication.IfTagValue,
                ThenTagValue = tagImplication.ThenTagValue
            };
            await _tableClient.AddEntityAsync(entity);
            return tagImplication;
        }

        public async Task DeleteTagImplicationAsync(Guid addressSpaceId, string ifTagValue)
        {
            await _tableClient.DeleteEntityAsync(addressSpaceId.ToString(), ifTagValue);
        }

        public async Task<TagImplication> GetTagImplicationAsync(Guid addressSpaceId, string ifTagValue)
        {
            var entity = await _tableClient.GetEntityAsync<TagImplicationEntity>(addressSpaceId.ToString(), ifTagValue);
            return new TagImplication
            {
                AddressSpaceId = Guid.Parse(entity.Value.PartitionKey),
                IfTagValue = entity.Value.RowKey,
                ThenTagValue = entity.Value.ThenTagValue
            };
        }

        public async Task<IEnumerable<TagImplication>> GetTagImplicationsAsync(Guid addressSpaceId)
        {
            var entities = _tableClient.QueryAsync<TagImplicationEntity>(e => e.PartitionKey == addressSpaceId.ToString());
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
