using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;

namespace Ipam.DataAccess.Repositories.Decorators
{
    public class CachingIpNodeRepository : CachingRepositoryDecorator<IIpNodeRepository>, IIpNodeRepository
    {
        private readonly IIpNodeRepository _repository;

        public CachingIpNodeRepository(
            IIpNodeRepository repository,
            IMemoryCache cache,
            IOptions<DataAccessOptions> options)
            : base(repository, cache, options)
        {
            _repository = repository;
        }

        public async Task<IpNode> GetByIdAsync(string addressSpaceId, string ipId)
        {
            return await WithCache(
                $"ipnode:{addressSpaceId}:{ipId}",
                () => _repository.GetByIdAsync(addressSpaceId, ipId));
        }

        // ...其他接口方法的实现...
    }
}
