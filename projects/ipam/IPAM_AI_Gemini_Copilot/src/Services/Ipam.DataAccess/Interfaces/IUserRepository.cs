
using System.Threading.Tasks;
using Ipam.Core;

namespace Ipam.DataAccess.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserAsync(string username);
        Task<User> CreateUserAsync(User user);
    }
}
