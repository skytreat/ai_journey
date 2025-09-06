using Microsoft.Extensions.Caching.Memory;
using Ipam.DataAccess.Configuration;
using Microsoft.Extensions.Options;

namespace Ipam.DataAccess.Repositories.Decorators
{
    /// <summary>
    /// Repository decorator that adds caching capabilities
    /// </summary>
    public class CachingRepositoryDecorator<TRepository>
    {
        private readonly TRepository _inner;
        private readonly IMemoryCache _cache;
        private readonly DataAccessOptions _options;

        public CachingRepositoryDecorator(
            TRepository inner,
            IMemoryCache cache,
            IOptions<DataAccessOptions> options)
        {
            _inner = inner;
            _cache = cache;
            _options = options.Value;
        }

        protected async Task<T> WithCache<T>(string key, Func<Task<T>> factory)
        {
            if (!_options.EnableCaching)
                return await factory();

            return await _cache.GetOrCreateAsync(
                key,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _options.CacheDuration;
                    return await factory();
                });
        }
    }
}
