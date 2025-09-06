using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Repositories.Decorators
{
    public class CachingIpNodeRepository : CachingRepositoryDecorator<IIpNodeRepository>, IIpNodeRepository
    {
        private readonly IIpNodeRepository _repository;
        private readonly IMemoryCache _memoryCache;

        public CachingIpNodeRepository(
            IIpNodeRepository repository,
            IMemoryCache cache,
            IOptions<DataAccessOptions> options)
            : base(repository, cache, options)
        {
            _repository = repository;
            _memoryCache = cache;
        }

        public async Task<IpNode> GetByIdAsync(string addressSpaceId, string ipId)
        {
            return await WithCache(
                $"ipnode:{addressSpaceId}:{ipId}",
                () => _repository.GetByIdAsync(addressSpaceId, ipId));
        }

        public async Task<IEnumerable<IpNode>> GetByPrefixAsync(string addressSpaceId, string cidr)
        {
            return await WithCache(
                $"ipnode:prefix:{addressSpaceId}:{cidr}",
                () => _repository.GetByPrefixAsync(addressSpaceId, cidr));
        }

        public async Task<IEnumerable<IpNode>> GetByTagsAsync(string addressSpaceId, Dictionary<string, string> tags)
        {
            var tagKey = string.Join(",", tags.Select(t => $"{t.Key}={t.Value}"));
            return await WithCache(
                $"ipnode:tags:{addressSpaceId}:{tagKey}",
                () => _repository.GetByTagsAsync(addressSpaceId, tags));
        }

        public async Task<IEnumerable<IpNode>> GetChildrenAsync(string addressSpaceId, string parentId)
        {
            return await WithCache(
                $"ipnode:children:{addressSpaceId}:{parentId}",
                () => _repository.GetChildrenAsync(addressSpaceId, parentId));
        }

        public async Task<IpNode> CreateAsync(IpNode ipNode)
        {
            var result = await _repository.CreateAsync(ipNode);
            // Invalidate related cache entries
            _memoryCache.Remove($"ipnode:{ipNode.PartitionKey}:{ipNode.RowKey}");
            return result;
        }

        public async Task<IpNode> UpdateAsync(IpNode ipNode)
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
