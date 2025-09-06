using Azure.Data.Tables;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Tests.TestHelpers
{
    /// <summary>
    /// Mock implementation of TableClient for testing
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class MockTableClient
    {
        private readonly Dictionary<string, Dictionary<string, ITableEntity>> _data;

        public MockTableClient()
        {
            _data = new Dictionary<string, Dictionary<string, ITableEntity>>();
        }

        public Task<T> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : ITableEntity
        {
            if (!_data.ContainsKey(entity.PartitionKey))
            {
                _data[entity.PartitionKey] = new Dictionary<string, ITableEntity>();
            }

            _data[entity.PartitionKey][entity.RowKey] = entity;
            return Task.FromResult(entity);
        }

        public Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey, 
            CancellationToken cancellationToken = default) where T : class, ITableEntity
        {
            if (_data.TryGetValue(partitionKey, out var partition))
            {
                if (partition.TryGetValue(rowKey, out var entity))
                {
                    return Task.FromResult(entity as T);
                }
            }
            return Task.FromResult<T?>(null);
        }
    }
}
