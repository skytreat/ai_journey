using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FundRecommendationAPI.Services
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(object id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<int> CountAsync();
        IQueryable<T> Query();
    }
}
