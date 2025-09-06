using Ipam.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for tag repository operations
    /// </summary>
    public interface ITagRepository
    {
        Task<Tag> GetByNameAsync(string addressSpaceId, string tagName);
        Task<IEnumerable<Tag>> GetAllAsync(string addressSpaceId);
        Task<IEnumerable<Tag>> SearchByNameAsync(string addressSpaceId, string nameFilter);
        Task<Tag> CreateAsync(Tag tag);
        Task<Tag> UpdateAsync(Tag tag);
        Task DeleteAsync(string addressSpaceId, string tagName);
    }
}
