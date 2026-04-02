using System.Collections.Generic;
using System.Threading.Tasks;

namespace FundRecommendationAPI.Services
{
    public interface IFundAnalysisService
    {
        Task<List<FundRanking>> GetFundRanking(string period = "month", int limit = 10, string order = "desc");
        Task<List<FundChangeRanking>> GetFundChangeRanking(string period = "month", int limit = 10, string type = "absolute");
        Task<List<FundConsistency>> GetFundConsistency(string startDate = "2023-01-01", string endDate = "2024-01-01", int limit = 10);
        Task<List<FundMultiFactorScore>> GetFundMultiFactorScore(int limit = 10, string[] factors = null);
        Task<List<FundComparison>> CompareFunds(string[] fundIds);
        decimal CalculateAdjustedNav(decimal nav, decimal accumulatedNav);
    }
}
