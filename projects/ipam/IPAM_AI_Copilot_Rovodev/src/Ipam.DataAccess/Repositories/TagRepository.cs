using Azure.Data.Tables;
using Ipam.DataAccess.Extensions;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Validation;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipam.DataAccess.Entities;

namespace Ipam.DataAccess.Repositories
{
    /// <summary>
    /// Implementation of tag repository using Azure Table Storage
    /// </summary>
    public class TagRepository : BaseRepository<TagEntity>, ITagRepository
    {
        private const string TableName = "Tags";

        public TagRepository(IConfiguration configuration)
            : base(configuration, TableName)
        {
        }

        public async Task<TagEntity> GetByNameAsync(string addressSpaceId, string tagName)
        {
            return await TableClient.GetEntityAsync<TagEntity>(addressSpaceId, tagName);
        }

        public async Task<IEnumerable<TagEntity>> GetAllAsync(string addressSpaceId)
        {
            var query = TableClient.QueryAsync<TagEntity>(t => t.PartitionKey == addressSpaceId);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<TagEntity>> SearchByNameAsync(string addressSpaceId, string nameFilter)
        {
            var query = TableClient.QueryAsync<TagEntity>(t => 
                t.PartitionKey == addressSpaceId && t.RowKey.Contains(nameFilter));
            
            return await query.ToListAsync();
        }

        public async Task<TagEntity> CreateAsync(TagEntity tag)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
            {
                if (tag.Type == "Inheritable" && tag.Implies != null)
                {
                    IpamValidator.ValidateTagImplications(tag.Implies);
                }

                await TableClient.AddEntityAsync(tag);
                return tag;
            });
        }

        public async Task<TagEntity> UpdateAsync(TagEntity tag)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
            {
                if (tag.Type == "Inheritable" && tag.Implies != null)
                {
                    IpamValidator.ValidateTagImplications(tag.Implies);
                }

                await TableClient.UpdateEntityAsync(tag, tag.ETag);
                return tag;
            });
        }

        public async Task DeleteAsync(string addressSpaceId, string tagName)
        {
            await TableClient.DeleteEntityAsync(addressSpaceId, tagName);
        }
    }
}