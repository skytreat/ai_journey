using Azure.Data.Tables;
using Ipam.DataAccess.Extensions;
using Ipam.DataAccess.Interfaces;
using Ipam.ServiceContract.DTOs;
using Ipam.DataAccess.Validation;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipam.DataAccess.Entities;
using Ipam.ServiceContract.Models;

namespace Ipam.DataAccess.Repositories
{
    /// <summary>
    /// Implementation of IP node repository using Azure Table Storage
    /// </summary>
    public class IpAllocationRepository : BaseRepository<IpAllocationEntity>, IIpAllocationRepository
    {
        private const string TableName = "IpNodes";

        public IpAllocationRepository(IConfiguration configuration)
            : base(configuration, TableName)
        {
        }

        public async Task<IpAllocationEntity> GetByIdAsync(string addressSpaceId, string ipId)
        {
            return await TableClient.GetEntityAsync<IpAllocationEntity>(addressSpaceId, ipId);
        }

        public async Task<IList<IpAllocationEntity>> GetAllAsync(string addressSpaceId)
        {
            var query = TableClient.QueryAsync<IpAllocationEntity>(n => n.PartitionKey == addressSpaceId);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetByPrefixAsync(string addressSpaceId, string cidr)
        {
            // 首先尝试精确匹配
            var exactMatches = await TableClient.QueryAsync<IpAllocationEntity>(n =>
                n.PartitionKey == addressSpaceId && n.Prefix == cidr).ToListAsync();

            if (exactMatches.Any())
            {
                return exactMatches;
            }

            // 如果没有精确匹配，查找最近的父节点
            var parent = await FindClosestParentByPrefix(addressSpaceId, cidr);
            return parent != null ? new List<IpAllocationEntity> { parent } : Enumerable.Empty<IpAllocationEntity>();
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetByTagsAsync(string addressSpaceId, Dictionary<string, string> tags)
        {
            var query = TableClient.QueryAsync<IpAllocationEntity>(n => n.PartitionKey == addressSpaceId);
            var results = new List<IpAllocationEntity>();

            await foreach (var node in query)
            {
                if (tags.All(t => node.Tags.ContainsKey(t.Key) && node.Tags[t.Key] == t.Value))
                {
                    results.Add(node);
                }
            }

            return results;
        }

        public async Task<IEnumerable<IpAllocationEntity>> GetChildrenAsync(string addressSpaceId, string parentId)
        {
            var query = TableClient.QueryAsync<IpAllocationEntity>(n =>
                n.PartitionKey == addressSpaceId && n.ParentId == parentId);

            return await query.ToListAsync();
        }

        public async Task<IpAllocationEntity> CreateAsync(IpAllocationEntity ipNode)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
            {
                IpamValidator.ValidateCidr(ipNode.Prefix);

                var parent = await FindClosestParentByPrefix(ipNode.PartitionKey, ipNode.Prefix);
                if (parent != null)
                {
                    ipNode.ParentId = parent.Id;
                    IpamValidator.ValidateTagInheritance(parent.Tags, ipNode.Tags);
                }

                await TableClient.AddEntityAsync(ipNode);
                return ipNode;
            });
        }

        public async Task<IpAllocationEntity> UpdateAsync(IpAllocationEntity ipNode)
        {
            await TableClient.UpdateEntityAsync(ipNode, ipNode.ETag);
            return ipNode;
        }

        public async Task DeleteAsync(string addressSpaceId, string ipId)
        {
            await TableClient.DeleteEntityAsync(addressSpaceId, ipId);
        }
        
        private async Task<IpAllocationEntity> FindClosestParentByPrefix(string addressSpaceId, string targetCidr)
        {
            var targetPrefix = new Prefix(targetCidr);
            var closestParent = default(IpAllocationEntity);
            var maxMatchingLength = -1;

            var query = TableClient.QueryAsync<IpAllocationEntity>(n => n.PartitionKey == addressSpaceId);
            await foreach (var node in query)
            {
                var nodePrefix = new Prefix(node.Prefix);
                if (nodePrefix.IsSupernetOf(targetPrefix) && 
                    nodePrefix.PrefixLength > maxMatchingLength)
                {
                    closestParent = node;
                    maxMatchingLength = nodePrefix.PrefixLength;
                }
            }

            return closestParent;
        }
    }
}