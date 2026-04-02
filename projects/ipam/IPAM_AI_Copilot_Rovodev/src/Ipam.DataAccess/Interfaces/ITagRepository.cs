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
        Task<OptimizedTagEntity> GetByNameAsync(string addressSpaceId, string tagName);
        Task<IEnumerable<OptimizedTagEntity>> GetAllAsync(string addressSpaceId);
        Task<IEnumerable<OptimizedTagEntity>> SearchByNameAsync(string addressSpaceId, string nameFilter);
        Task<OptimizedTagEntity> CreateAsync(OptimizedTagEntity tag);
        Task<OptimizedTagEntity> UpdateAsync(OptimizedTagEntity tag);
        Task DeleteAsync(string addressSpaceId, string tagName);
    }
}
