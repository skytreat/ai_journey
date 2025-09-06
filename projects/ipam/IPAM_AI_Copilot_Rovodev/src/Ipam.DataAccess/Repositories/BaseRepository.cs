using Azure.Data.Tables;
using Ipam.DataAccess.Extensions;
using Ipam.DataAccess.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Repositories
{
    /// <summary>
    /// Base repository class for Azure Table Storage operations
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public abstract class BaseRepository<T> where T : class, ITableEntity, IEntity
    {
        private readonly TableClient _tableClient;

        /// <summary>
        /// Gets the TableClient instance for derived classes
        /// </summary>
        protected TableClient TableClient => _tableClient;

        protected BaseRepository(IConfiguration configuration, string tableName)
        {
            var connectionString = configuration.GetConnectionString("AzureTableStorage");
            _tableClient = new TableClient(connectionString, tableName);
            _tableClient.CreateIfNotExists();
        }

        protected async Task<T> AddEntityAsync(T entity)
        {
            return await _tableClient.ExecuteWithRetryAsync(async () =>
            {
                await _tableClient.AddEntityAsync(entity);
                return entity;
            });
        }

        protected async Task<T> UpdateEntityAsync(T entity)
        {
            return await _tableClient.ExecuteWithRetryAsync(async () =>
            {
                await _tableClient.UpdateEntityAsync(entity, entity.ETag);
                return entity;
            });
        }

        protected async Task DeleteEntityAsync(string partitionKey, string rowKey)
        {
            await _tableClient.ExecuteWithRetryAsync(async () =>
            {
                await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
                return true;
            });
        }
    }
}
