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
    public class CachingIpNodeRepository : CachingRepositoryDecorator<IIpAllocationRepository>, IIpAllocationRepository
    {
        private readonly IIpAllocationRepository _repository;
        private readonly IMemoryCache _memoryCache;

        public CachingIpNodeRepository(
            IIpAllocationRepository repository,
            IMemoryCache cache,
            IOptions<DataAccessOptions> options)
            : base(repository, cache, options)
        {
            _repository = repository;
            _memoryCache = cache;
        }

        public async Task<IpAllocationEntity> GetByIdAsync(string addressSpaceId, string ipId)
        {
            return await WithCache(
                $"ipnode:{addressSpaceId}:{ipId}",
                () => _repository.GetByIdAsync(addressSpaceId, ipId));
        }

        public async Task<IList<IpAllocationEntity>> GetAllAsync(string addressSpaceId)
        {
            return await WithCache(
                $"ipnode:all:{addressSpaceId}",
                () => _repository.GetAllAsync(addressSpaceId));
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetByPrefixAsync(string addressSpaceId, string cidr)
        {
            return await WithCache(
                $"ipnode:prefix:{addressSpaceId}:{cidr}",
                () => _repository.GetByPrefixAsync(addressSpaceId, cidr));
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetByTagsAsync(string addressSpaceId, Dictionary<string, string> tags)
        {
            var tagKey = string.Join(",", tags.Select(t => $"{t.Key}={t.Value}"));
            return await WithCache(
                $"ipnode:tags:{addressSpaceId}:{tagKey}",
                () => _repository.GetByTagsAsync(addressSpaceId, tags));
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetChildrenAsync(string addressSpaceId, string parentId)
        {
            return await WithCache(
                $"ipnode:children:{addressSpaceId}:{parentId}",
                () => _repository.GetChildrenAsync(addressSpaceId, parentId));
        }

        public async Task<IpAllocationEntity> CreateAsync(IpAllocationEntity ipNode)
        {
            var result = await _repository.CreateAsync(ipNode);
            
            // Thread-safe cache invalidation for creation
            InvalidateCacheForNode(result);
            
            return result;
        }

        public async Task<IpAllocationEntity> UpdateAsync(IpAllocationEntity ipNode)
        {
            var result = await _repository.UpdateAsync(ipNode);
            
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
                _memoryCache.Remove($"ipnode:{ipNode.PartitionKey}:{ipNode.RowKey}");
                
                // Invalidate related cache entries that might be affected
                _memoryCache.Remove($"ipnode:all:{ipNode.PartitionKey}");
                _memoryCache.Remove($"ipnode:prefix:{ipNode.PartitionKey}:{ipNode.Prefix}");
                
                // Invalidate parent and children caches
                if (!string.IsNullOrEmpty(ipNode.ParentId))
                {
                    _memoryCache.Remove($"ipnode:children:{ipNode.PartitionKey}:{ipNode.ParentId}");
                }
                
                // Invalidate this node's children cache
                _memoryCache.Remove($"ipnode:children:{ipNode.PartitionKey}:{ipNode.RowKey}");
                
                // Invalidate tag-based caches (simplified - in production, might need more sophisticated invalidation)
                foreach (var tag in ipNode.Tags)
                {
                    var tagKey = $"{tag.Key}={tag.Value}";
                    _memoryCache.Remove($"ipnode:tags:{ipNode.PartitionKey}:{tagKey}");
                }
            }
        }

        public async Task DeleteAsync(string addressSpaceId, string ipId)
        {
            await _repository.DeleteAsync(addressSpaceId, ipId);
            
            // Thread-safe cache invalidation for deletion
            lock (_cacheInvalidationLock)
            {
                // Invalidate specific node cache
                _memoryCache.Remove($"ipnode:{addressSpaceId}:{ipId}");
                
                // Invalidate broader cache entries
                _memoryCache.Remove($"ipnode:all:{addressSpaceId}");
                _memoryCache.Remove($"ipnode:children:{addressSpaceId}:{ipId}");
                
                // Note: We can't easily invalidate parent/prefix caches without the full entity
                // In production, consider maintaining a cache dependency map
            }
        }
    }
}