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
    /// Implementation of tag repository using Azure Table Storage with optimized performance
    /// </summary>
    public class TagRepository : BaseRepository<OptimizedTagEntity>, ITagRepository
    {
        private const string TableName = "Tags";

        public TagRepository(IConfiguration configuration)
            : base(configuration, TableName)
        {
        }

        public async Task<OptimizedTagEntity> GetByNameAsync(string addressSpaceId, string tagName)
        {
            return await TableClient.GetEntityAsync<OptimizedTagEntity>(addressSpaceId, tagName);
        }

        public async Task<IEnumerable<OptimizedTagEntity>> GetAllAsync(string addressSpaceId)
        {
            var query = TableClient.QueryAsync<OptimizedTagEntity>(t => t.PartitionKey == addressSpaceId);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<OptimizedTagEntity>> SearchByNameAsync(string addressSpaceId, string nameFilter)
        {
            var query = TableClient.QueryAsync<OptimizedTagEntity>(t => 
                t.PartitionKey == addressSpaceId && t.RowKey.Contains(nameFilter));
            
            return await query.ToListAsync();
        }

        public async Task<OptimizedTagEntity> CreateAsync(OptimizedTagEntity tag)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
            {
                if (tag.Type == "Inheritable" && tag.Implies != null)
                {
                    IpamValidator.ValidateTagImplications(tag.Implies);
                }

                // PERFORMANCE: Flush cached changes to JSON storage fields before saving
                tag.FlushChanges();
                
                await TableClient.AddEntityAsync(tag);
                return tag;
            });
        }

        public async Task<OptimizedTagEntity> UpdateAsync(OptimizedTagEntity tag)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
            {
                if (tag.Type == "Inheritable" && tag.Implies != null)
                {
                    IpamValidator.ValidateTagImplications(tag.Implies);
                }

                // PERFORMANCE: Flush cached changes to JSON storage fields before saving
                tag.FlushChanges();
                
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