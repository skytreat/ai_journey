
using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Ipam.Core;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private const string TableName = "Users";
        private readonly TableClient _tableClient;

        public UserRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            var entity = new UserEntity
            {
                PartitionKey = "SYSTEM",
                RowKey = user.Username,
                PasswordHash = user.PasswordHash
            };
            try
            {
                await _tableClient.AddEntityAsync(entity);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create user in Azure Table Storage: {ex.Message}", ex);
            }
            return user;
        }

        public async Task<User> GetUserAsync(string username)
        {
            try
            {
                var entity = await _tableClient.GetEntityAsync<UserEntity>("SYSTEM", username);
                return new User
                {
                    Username = entity.Value.RowKey,
                    PasswordHash = entity.Value.PasswordHash
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get user from Azure Table Storage: {ex.Message}", ex);
            }
        }
    }
}
