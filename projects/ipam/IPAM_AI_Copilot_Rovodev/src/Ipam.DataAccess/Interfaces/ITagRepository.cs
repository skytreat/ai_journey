using Ipam.DataAccess.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for tag repository operations
    /// </summary>
    public interface ITagRepository
    {
        Task<TagEntity> GetByNameAsync(string addressSpaceId, string tagName);
        Task<IEnumerable<TagEntity>> GetAllAsync(string addressSpaceId);
        Task<IEnumerable<TagEntity>> SearchByNameAsync(string addressSpaceId, string nameFilter);
        Task<TagEntity> CreateAsync(TagEntity tag);
        Task<TagEntity> UpdateAsync(TagEntity tag);
        Task DeleteAsync(string addressSpaceId, string tagName);
    }
}
