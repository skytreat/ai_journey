using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.Models;

namespace Ipam.DataAccess.Repositories.Decorators
{
    public class CachingIpAllocationRepository : CachingRepositoryDecorator<IIpAllocationRepository>, IIpAllocationRepository
    {
        public CachingIpAllocationRepository(
            IIpAllocationRepository repository,
            IMemoryCache cache,
            IOptions<DataAccessOptions> options)
            : base(repository, cache, options)
        {
            // Base class now handles repository and cache storage
        }

        public async Task<IpAllocationEntity> GetByIdAsync(string addressSpaceId, string ipId)
        {
            return await WithCache(
                $"ipnode:{addressSpaceId}:{ipId}",
                () => Repository.GetByIdAsync(addressSpaceId, ipId));
        }

        public async Task<IList<IpAllocationEntity>> GetAllAsync(string addressSpaceId)
        {
            return await WithCache(
                $"ipnode:all:{addressSpaceId}",
                () => Repository.GetAllAsync(addressSpaceId));
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetByPrefixAsync(string addressSpaceId, string cidr)
        {
            return await WithCache(
                $"ipnode:prefix:{addressSpaceId}:{cidr}",
                () => Repository.GetByPrefixAsync(addressSpaceId, cidr));
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetByTagsAsync(string addressSpaceId, Dictionary<string, string> tags)
        {
            var tagKey = string.Join(",", tags.Select(t => $"{t.Key}={t.Value}"));
            return await WithCache(
                $"ipnode:tags:{addressSpaceId}:{tagKey}",
                () => Repository.GetByTagsAsync(addressSpaceId, tags));
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetChildrenAsync(string addressSpaceId, string parentId)
        {
            return await WithCache(
                $"ipnode:children:{addressSpaceId}:{parentId}",
                () => Repository.GetChildrenAsync(addressSpaceId, parentId));
        }

        public async Task<IpAllocationEntity> CreateAsync(IpAllocationEntity ipNode)
        {
            var result = await Repository.CreateAsync(ipNode);
            
            // Thread-safe cache invalidation for creation
            InvalidateCacheForNode(result);
            
            return result;
        }

        public async Task<IpAllocationEntity> UpdateAsync(IpAllocationEntity ipNode)
        {
            var result = await Repository.UpdateAsync(ipNode);
            
            // Thread-safe cache invalidation
            InvalidateCacheForNode(ipNode);
            
            return result;
        }

        private readonly object _cacheInvalidationLock = new object();

        private void InvalidateCacheForNode(IpAllocationEntity ipNode)
        {
            lock (_cacheInvalidationLock)
            {
                // Invalidate specific node cache
                Cache.Remove($"ipnode:{ipNode.PartitionKey}:{ipNode.RowKey}");
                
                // Invalidate related cache entries that might be affected
                Cache.Remove($"ipnode:all:{ipNode.PartitionKey}");
                Cache.Remove($"ipnode:prefix:{ipNode.PartitionKey}:{ipNode.Prefix}");
                
                // Invalidate parent and children caches
                if (!string.IsNullOrEmpty(ipNode.ParentId))
                {
                    Cache.Remove($"ipnode:children:{ipNode.PartitionKey}:{ipNode.ParentId}");
                }
                
                // Invalidate this node's children cache
                Cache.Remove($"ipnode:children:{ipNode.PartitionKey}:{ipNode.RowKey}");
                
                // Invalidate tag-based caches (simplified - in production, might need more sophisticated invalidation)
                foreach (var tag in ipNode.Tags)
                {
                    var tagKey = $"{tag.Key}={tag.Value}";
                    Cache.Remove($"ipnode:tags:{ipNode.PartitionKey}:{tagKey}");
                }
            }
        }

        public async Task DeleteAsync(string addressSpaceId, string ipId)
        {
            await Repository.DeleteAsync(addressSpaceId, ipId);
            
            // Thread-safe cache invalidation for deletion
            lock (_cacheInvalidationLock)
            {
                // Invalidate specific node cache
                Cache.Remove($"ipnode:{addressSpaceId}:{ipId}");
                
                // Invalidate broader cache entries
                Cache.Remove($"ipnode:all:{addressSpaceId}");
                Cache.Remove($"ipnode:children:{addressSpaceId}:{ipId}");
                
                // Note: We can't easily invalidate parent/prefix caches without the full entity
                // In production, consider maintaining a cache dependency map
            }
        }
    }
}