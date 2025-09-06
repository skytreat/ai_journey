using Ipam.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for IP node repository operations
    /// </summary>
    public interface IIpNodeRepository
    {
        Task<IpNode> GetByIdAsync(string addressSpaceId, string ipId);
        Task<IEnumerable<IpNode>> GetByPrefixAsync(string addressSpaceId, string cidr);
        Task<IEnumerable<IpNode>> GetByTagsAsync(string addressSpaceId, Dictionary<string, string> tags);
        Task<IEnumerable<IpNode>> GetChildrenAsync(string addressSpaceId, string parentId);
        Task<IpNode> CreateAsync(IpNode ipNode);
        Task<IpNode> UpdateAsync(IpNode ipNode);
        Task DeleteAsync(string addressSpaceId, string ipId);
    }
}
