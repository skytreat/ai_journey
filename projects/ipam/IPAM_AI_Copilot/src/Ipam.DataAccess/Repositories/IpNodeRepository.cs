using Azure.Data.Tables;
using Ipam.DataAccess.Extensions;
using Ipam.DataAccess.Interfaces;
using Ipam.DataAccess.Models;
using Ipam.DataAccess.Validation;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Repositories
{
    /// <summary>
    /// Implementation of IP node repository using Azure Table Storage
    /// </summary>
    public class IpNodeRepository : BaseRepository<IpNode>, IIpNodeRepository
    {
        private const string TableName = "IpNodes";

        public IpNodeRepository(IConfiguration configuration)
            : base(configuration, TableName)
        {
        }

        public async Task<IpNode> GetByIdAsync(string addressSpaceId, string ipId)
        {
            return await TableClient.GetEntityAsync<IpNode>(addressSpaceId, ipId);
        }

        public async Task<IEnumerable<IpNode>> GetByPrefixAsync(string addressSpaceId, string cidr)
        {
            // 首先尝试精确匹配
            var exactMatches = await TableClient.QueryAsync<IpNode>(n => 
                n.PartitionKey == addressSpaceId && n.Prefix == cidr).ToListAsync();

            if (exactMatches.Any())
            {
                return exactMatches;
            }

            // 如果没有精确匹配，查找最近的父节点
            var parent = await FindClosestParentByPrefix(addressSpaceId, cidr);
            return parent != null ? new[] { parent } : Enumerable.Empty<IpNode>();
        }

        private async Task<IpNode> FindClosestParentByPrefix(string addressSpaceId, string targetCidr)
        {
            var targetPrefix = new Prefix(targetCidr);
            var closestParent = default(IpNode);
            var maxMatchingLength = -1;

            var query = TableClient.QueryAsync<IpNode>(n => n.PartitionKey == addressSpaceId);
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

        public async Task<IEnumerable<IpNode>> GetByTagsAsync(string addressSpaceId, Dictionary<string, string> tags)
        {
            var query = TableClient.QueryAsync<IpNode>(n => n.PartitionKey == addressSpaceId);
            var results = new List<IpNode>();

            await foreach (var node in query)
            {
                if (tags.All(t => node.Tags.ContainsKey(t.Key) && node.Tags[t.Key] == t.Value))
                {
                    results.Add(node);
                }
            }

            return results;
        }

        public async Task<IEnumerable<IpNode>> GetChildrenAsync(string addressSpaceId, string parentId)
        {
            var query = TableClient.QueryAsync<IpNode>(n => 
                n.PartitionKey == addressSpaceId && n.ParentId == parentId);
            
            return await query.ToListAsync();
        }

        public async Task<IpNode> CreateAsync(IpNode ipNode)
        {
            return await TableClient.ExecuteWithRetryAsync(async () =>
            {
                IpamValidator.ValidateCidr(ipNode.Prefix);
                
                var parent = await FindParentNode(ipNode.PartitionKey, ipNode.Prefix);
                if (parent != null)
                {
                    ipNode.ParentId = parent.RowKey;
                    IpamValidator.ValidateTagInheritance(parent.Tags, ipNode.Tags);
                }

                await TableClient.AddEntityAsync(ipNode);
                return ipNode;
            });
        }

        private async Task<IpNode> FindParentNode(string addressSpaceId, string cidr)
        {
            // Find the closest parent node based on CIDR prefix
            // ...implementation details...
            return null;
        }

        public async Task<IpNode> UpdateAsync(IpNode ipNode)
        {
            await TableClient.UpdateEntityAsync(ipNode, ipNode.ETag);
            return ipNode;
        }

        public async Task DeleteAsync(string addressSpaceId, string ipId)
        {
            await TableClient.DeleteEntityAsync(addressSpaceId, ipId);
        }
    }
}
                {
                    ipNode.ParentId = parent.RowKey;
                    IpamValidator.ValidateTagInheritance(parent.Tags, ipNode.Tags);
                }

                await TableClient.AddEntityAsync(ipNode);
                return ipNode;
            });
        }

        private async Task<IpNode> FindParentNode(string addressSpaceId, string cidr)
        {
            // Find the closest parent node based on CIDR prefix
            // ...implementation details...
            return null;
        }

        public async Task<IpNode> UpdateAsync(IpNode ipNode)
        {
            await TableClient.UpdateEntityAsync(ipNode, ipNode.ETag);
            return ipNode;
        }

        public async Task DeleteAsync(string addressSpaceId, string ipId)
        {
            await TableClient.DeleteEntityAsync(addressSpaceId, ipId);
        }
    }
}

