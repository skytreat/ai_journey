using System.Threading;
using System.Threading.Tasks;

namespace FundRecommendationAPI.Services
{
    public interface ISystemService
    {
        Task<object> GetSystemStatusAsync();
        Task<object> TriggerDataUpdateAsync(string updateType = "full");
        Task<object> GetUpdateHistoryAsync(int limit = 10);
        Task RealDataUpdateAsync(string updateType = "full", CancellationToken cancellationToken = default);
    }
}
