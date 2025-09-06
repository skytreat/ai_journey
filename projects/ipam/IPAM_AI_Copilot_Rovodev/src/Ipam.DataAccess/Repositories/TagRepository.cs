using Azure.Data.Tables;
using Ipam.DataAccess.Extensions;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Validation;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Repositories
{
    /// <summary>
    /// Implementation of tag repository using Azure Table Storage
    /// </summary>
    public class TagRepository : BaseRepository<Tag>, ITagRepository
    {
        private const string TableName = "Tags";

        public TagRepository(IConfiguration configuration)
            : base(configuration, TableName)
        {
        }

        public async Task<Tag> GetByNameAsync(string addressSpaceId, string tagName)
        {
            return await TableClient.GetEntityAsync<Tag>(addressSpaceId, tagName);
        }

        public async Task<IEnumerable<Tag>> GetAllAsync(string addressSpaceId)
        {
            var query = TableClient.QueryAsync<Tag>(t => t.PartitionKey == addressSpaceId);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Tag>> SearchByNameAsync(string addressSpaceId, string nameFilter)
        {
            var query = TableClient.QueryAsync<Tag>(t => 
                t.PartitionKey == addressSpaceId && t.RowKey.Contains(nameFilter));
            
            return await query.ToListAsync();
        }

        public async Task<Tag> CreateAsync(Tag tag)
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

        public async Task<Tag> UpdateAsync(Tag tag)
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