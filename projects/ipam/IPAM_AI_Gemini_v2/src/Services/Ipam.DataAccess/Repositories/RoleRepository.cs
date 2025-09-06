
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Ipam.Core;
using Ipam.DataAccess.Entities;
using Ipam.DataAccess.Interfaces;

namespace Ipam.DataAccess.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private const string TableName = "UserRoles";
        private readonly TableClient _tableClient;

        public RoleRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<Role> CreateRoleAsync(Role role)
        {
            var entity = new RoleEntity
            {
                PartitionKey = role.Username,
                RowKey = role.AddressSpaceId.ToString(),
                Role = role.RoleName
            };
            await _tableClient.AddEntityAsync(entity);
            return role;
        }

        public async Task DeleteRoleAsync(string username, Guid addressSpaceId)
        {
            await _tableClient.DeleteEntityAsync(username, addressSpaceId.ToString());
        }

        public async Task<IEnumerable<Role>> GetRolesAsync(string username)
        {
            var entities = _tableClient.QueryAsync<RoleEntity>(e => e.PartitionKey == username);
            var roles = new List<Role>();
            await foreach (var entity in entities)
            {
                if (entity == null)
                {
                    continue;
                }

                roles.Add(new Role
                {
                    Username = entity.PartitionKey ?? string.Empty,
                    AddressSpaceId = Guid.TryParse(entity.RowKey ?? string.Empty, out Guid addressSpaceId) ? addressSpaceId : Guid.Empty,
                    RoleName = entity.Role ?? string.Empty
                });
            }
            return roles;
        }
    }
}
