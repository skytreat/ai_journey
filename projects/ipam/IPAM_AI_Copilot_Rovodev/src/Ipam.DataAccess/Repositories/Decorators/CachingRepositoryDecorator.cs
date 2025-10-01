using Microsoft.Extensions.Caching.Memory;
using Ipam.DataAccess.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Repositories.Decorators
{
    /// <summary>
    /// Repository decorator that adds caching capabilities
    /// </summary>
    public class CachingRepositoryDecorator<TRepository>
    {
        private readonly TRepository _innerRepository;
        private readonly IMemoryCache _cache;
        private readonly DataAccessOptions _options;

        public CachingRepositoryDecorator(
            TRepository inner,
            IMemoryCache cache,
            IOptions<DataAccessOptions> options)
        {
            _innerRepository = inner;
            _cache = cache;
            _options = options.Value;
        }

        /// <summary>
        /// Protected access to the inner repository for derived classes
        /// </summary>
        protected TRepository Repository => _innerRepository;

        /// <summary>
        /// Protected access to the memory cache for derived classes
        /// </summary>
        protected IMemoryCache Cache => _cache;

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
