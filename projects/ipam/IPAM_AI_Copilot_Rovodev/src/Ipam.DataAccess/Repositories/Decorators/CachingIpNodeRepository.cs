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
            // Invalidate related cache entries
            _memoryCache.Remove($"ipnode:{ipNode.PartitionKey}:{ipNode.RowKey}");
            return result;
        }

        public async Task<IpAllocationEntity> UpdateAsync(IpAllocationEntity ipNode)
        {
            var result = await _repository.UpdateAsync(ipNode);
            // Invalidate related cache entries
            _memoryCache.Remove($"ipnode:{ipNode.PartitionKey}:{ipNode.RowKey}");
            return result;
        }

        public async Task DeleteAsync(string addressSpaceId, string ipId)
        {
            await _repository.DeleteAsync(addressSpaceId, ipId);
            // Invalidate related cache entries
            _memoryCache.Remove($"ipnode:{addressSpaceId}:{ipId}");
        }
    }
}