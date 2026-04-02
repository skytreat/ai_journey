using System.Collections.Generic;
using System.Threading.Tasks;
using FundRecommendationAPI.Models;

namespace FundRecommendationAPI.Services
{
    public interface IFundDataService
    {
        Task<FundBasicInfo> UpdateFundBasicInfo(string fundCode);
        Task<List<FundNavHistory>> UpdateFundNavHistory(string fundCode, string startDate, string endDate);
        Task<List<FundPerformance>> UpdateFundPerformance(string fundCode);
        Task<List<FundManager>> UpdateFundManagers(string fundCode);
        Task<List<FundAssetScale>> UpdateFundAssetScale(string fundCode, string startDate, string endDate);
        Task<List<FundPurchaseStatus>> UpdateFundPurchaseStatus(string fundCode);
        Task<List<FundRedemptionStatus>> UpdateFundRedemptionStatus(string fundCode);
        Task<List<FundCorporateActions>> UpdateFundCorporateActions(string fundCode);
        Task<bool> HasNewCorporateActionsAsync(string fundCode, string sinceDate);
        Task UpdateRecentPerformanceAsync(string fundCode);
    }
}
