using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FundRecommendationAPI.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FundRecommendationAPI.Services
{
    public class FundDataService : IFundDataService
    {
        private readonly string _pythonScriptPath;
        private readonly ILogger<FundDataService> _logger;
        private readonly IRepository<FundBasicInfo> _fundRepository;
        private readonly IRepository<FundNavHistory> _navHistoryRepository;
        private readonly IRepository<FundPerformance> _performanceRepository;
        private readonly IRepository<FundManager> _managerRepository;
        private readonly IRepository<FundAssetScale> _assetScaleRepository;
        private readonly IRepository<FundPurchaseStatus> _purchaseStatusRepository;
        private readonly IRepository<FundRedemptionStatus> _redemptionStatusRepository;
        private readonly IRepository<FundCorporateActions> _corporateActionsRepository;

        public FundDataService(
            IRepository<FundBasicInfo> fundRepository, 
            IRepository<FundNavHistory> navHistoryRepository,
            IRepository<FundPerformance> performanceRepository,
            IRepository<FundManager> managerRepository,
            IRepository<FundAssetScale> assetScaleRepository,
            IRepository<FundPurchaseStatus> purchaseStatusRepository,
            IRepository<FundRedemptionStatus> redemptionStatusRepository,
            IRepository<FundCorporateActions> corporateActionsRepository,
            ILogger<FundDataService> logger)
        {
            _pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Services", "fund_data_collector.py");
            _logger = logger;
            _fundRepository = fundRepository ?? throw new ArgumentNullException(nameof(fundRepository));
            _navHistoryRepository = navHistoryRepository ?? throw new ArgumentNullException(nameof(navHistoryRepository));
            _performanceRepository = performanceRepository ?? throw new ArgumentNullException(nameof(performanceRepository));
            _managerRepository = managerRepository ?? throw new ArgumentNullException(nameof(managerRepository));
            _assetScaleRepository = assetScaleRepository ?? throw new ArgumentNullException(nameof(assetScaleRepository));
            _purchaseStatusRepository = purchaseStatusRepository ?? throw new ArgumentNullException(nameof(purchaseStatusRepository));
            _redemptionStatusRepository = redemptionStatusRepository ?? throw new ArgumentNullException(nameof(redemptionStatusRepository));
            _corporateActionsRepository = corporateActionsRepository ?? throw new ArgumentNullException(nameof(corporateActionsRepository));
        }

        // 执行Python脚本
        protected virtual string ExecutePythonScript(string command, params string[] args)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"-X utf8 \"{_pythonScriptPath}\" {command} {string.Join(" ", args)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process == null)
                {
                    throw new Exception("Failed to start Python process");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Python script failed with error: {error}");
                }

                return output;
            }
        }

        public async Task<FundBasicInfo> UpdateFundBasicInfo(string fundCode)
        {
            try
            {
                var output = ExecutePythonScript("get_basic_info", fundCode);
                var fundInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(output);

                if (fundInfo == null || fundInfo.ContainsKey("error"))
                {
                    _logger.LogWarning("Python script returned empty or error for fund {FundCode}", fundCode);
                    return null;
                }

                var fund = await _fundRepository.Query().FirstOrDefaultAsync(f => f.Code == fundCode);
                var isNew = fund == null;
                if (isNew)
                {
                    fund = new FundBasicInfo { Code = fundCode };
                }

                fund.Name = fundInfo.TryGetValue("基金名称", out var name) ? name.ToString() ?? "" : "";
                fund.FundType = fundInfo.TryGetValue("基金类型", out var type) ? type.ToString() ?? "" : "";
                fund.ShareType = "前端";
                fund.MainFundCode = fundCode;
                fund.EstablishDate = DateOnly.Parse(fundInfo.TryGetValue("成立日期", out var date) ? date.ToString() : DateTime.Now.ToString("yyyy-MM-dd"));
                fund.Manager = fundInfo.TryGetValue("基金经理", out var manager) ? manager.ToString() ?? "" : "";
                fund.Custodian = fundInfo.TryGetValue("基金托管人", out var custodian) ? custodian.ToString() ?? "" : "";
                fund.ManagementFeeRate = fundInfo.TryGetValue("管理费", out var fee) ? fee.ToString() ?? "" : "1.50%";
                fund.CustodianFeeRate = fundInfo.TryGetValue("托管费", out var custodianFee) ? custodianFee.ToString() ?? "" : "0.25%";
                fund.Benchmark = fundInfo.TryGetValue("业绩比较基准", out var benchmark) ? benchmark.ToString() ?? "" : "";
                fund.TrackingTarget = "";
                fund.InvestmentStyle = "";
                fund.RiskLevel = fundInfo.TryGetValue("风险等级", out var risk) ? risk.ToString() ?? "" : "";
                fund.UpdateTime = DateTime.Now;

                if (isNew)
                {
                    await _fundRepository.AddAsync(fund);
                }
                else
                {
                    await _fundRepository.UpdateAsync(fund);
                }

                return fund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update fund basic info: {FundCode}", fundCode);
                return null;
            }
        }

        // 获取并更新基金净值历史数据（只插入新记录，不更新已有记录）
        public async Task<List<FundNavHistory>> UpdateFundNavHistory(string fundCode, string startDate, string endDate)
        {
            try
            {
                var output = ExecutePythonScript("get_nav_history", fundCode, startDate, endDate);

                if (string.IsNullOrWhiteSpace(output) || output.StartsWith("{"))
                {
                    _logger.LogWarning("Python script returned empty or error for fund {FundCode} nav history", fundCode);
                    return new List<FundNavHistory>();
                }

                var navHistoryList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);

                var result = new List<FundNavHistory>();

                foreach (var item in navHistoryList)
                {
                    var date = DateOnly.Parse(item.TryGetValue("净值日期", out var dateValue) ? dateValue.ToString() : DateTime.Now.ToString("yyyy-MM-dd"));
                    var nav = decimal.Parse(item.TryGetValue("单位净值", out var navValue) ? navValue.ToString() : "0");
                    var accumulatedNav = decimal.Parse(item.TryGetValue("累计净值", out var accumulatedValue) ? accumulatedValue.ToString() : "0");
                    var dailyGrowthRate = item.TryGetValue("日增长率", out var growthValue) && growthValue.ToString() != ""
                        ? decimal.Parse(growthValue.ToString().Replace("%", "")) / 100
                        : (decimal?)null;

                    var navHistory = await _navHistoryRepository.Query().FirstOrDefaultAsync(n => n.Code == fundCode && n.Date == date);
                    if (navHistory == null)
                    {
                        navHistory = new FundNavHistory
                        {
                            Code = fundCode,
                            Date = date,
                            Nav = nav,
                            AccumulatedNav = accumulatedNav,
                            DailyGrowthRate = dailyGrowthRate,
                            UpdateTime = DateTime.Now
                        };
                        await _navHistoryRepository.AddAsync(navHistory);
                        result.Add(navHistory);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update fund nav history: {ex.Message}");
            }
        }

        public async Task<List<FundPerformance>> UpdateFundPerformance(string fundCode)
        {
            try
            {
                var endDate = DateOnly.FromDateTime(DateTime.Now);
                var navHistoryList = await _navHistoryRepository.Query()
                    .Where(n => n.Code == fundCode)
                    .OrderBy(n => n.Date)
                    .ToListAsync();

                if (navHistoryList.Count < 2)
                {
                    _logger.LogWarning("Not enough nav history data for fund {FundCode}", fundCode);
                    return new List<FundPerformance>();
                }

                var periods = new Dictionary<string, int>
                {
                    { "1周", 7 },
                    { "1月", 30 },
                    { "3月", 90 },
                    { "6月", 180 },
                    { "1年", 365 },
                    { "近1年", 365 },
                    { "近3年", 1095 }
                };

                var result = new List<FundPerformance>();

                foreach (var (periodName, days) in periods)
                {
                    var periodStartDate = endDate.AddDays(-days);
                    var periodNavHistory = navHistoryList.Where(n => n.Date >= periodStartDate).ToList();

                    if (periodNavHistory.Count < 2)
                        continue;

                    var periodData = CalculatePerformanceFromNavHistory(periodNavHistory, periodName);
                    if (periodData == null)
                        continue;

                    var existing = await _performanceRepository.Query()
                        .FirstOrDefaultAsync(p => p.Code == fundCode && p.PeriodType == periodData.PeriodType && p.PeriodValue == periodData.PeriodValue);

                    if (existing == null)
                    {
                        await _performanceRepository.AddAsync(periodData);
                    }
                    else
                    {
                        existing.NavGrowthRate = periodData.NavGrowthRate;
                        existing.MaxDrawdown = periodData.MaxDrawdown;
                        existing.Volatility = periodData.Volatility;
                        existing.SharpeRatio = periodData.SharpeRatio;
                        existing.SortinoRatio = periodData.SortinoRatio;
                        existing.CalmarRatio = periodData.CalmarRatio;
                        existing.AnnualReturn = periodData.AnnualReturn;
                        existing.UpdateTime = DateTime.Now;
                        await _performanceRepository.UpdateAsync(existing);
                        periodData.Id = existing.Id;
                    }
                    result.Add(periodData);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update fund performance: {FundCode}", fundCode);
                return new List<FundPerformance>();
            }
        }

        private FundPerformance? CalculatePerformanceFromNavHistory(List<FundNavHistory> navHistory, string periodName)
        {
            if (navHistory.Count < 2)
                return null;

            var orderedNav = navHistory.OrderBy(n => n.Date).ToList();
            var firstNav = orderedNav.First();
            var lastNav = orderedNav.Last();

            var navGrowthRate = firstNav.Nav > 0 ? (lastNav.Nav - firstNav.Nav) / firstNav.Nav : 0m;

            var dailyReturns = new List<decimal>();
            for (int i = 1; i < orderedNav.Count; i++)
            {
                if (orderedNav[i - 1].Nav > 0)
                {
                    var dailyReturn = (orderedNav[i].Nav - orderedNav[i - 1].Nav) / orderedNav[i - 1].Nav;
                    dailyReturns.Add(dailyReturn);
                }
            }

            if (dailyReturns.Count == 0)
                return null;

            var avgDailyReturn = dailyReturns.Average();
            var stdDailyReturn = CalculateStdDev(dailyReturns);

            var tradingDays = orderedNav.Count;
            var annualizedReturn = (decimal)Math.Pow((double)(1 + navGrowthRate), 365.0 / tradingDays) - 1m;
            var annualizedVolatility = stdDailyReturn * (decimal)Math.Sqrt(252);

            var maxDrawdown = CalculateMaxDrawdown(orderedNav);
            var downsideReturns = dailyReturns.Where(r => r < 0).ToList();
            var downsideStd = downsideReturns.Count > 0 ? CalculateStdDev(downsideReturns) : 0m;

            const decimal riskFreeRate = 0.03m;
            var sharpeRatio = annualizedVolatility > 0 ? (annualizedReturn - riskFreeRate) / annualizedVolatility : 0m;
            var sortinoRatio = downsideStd > 0 ? (annualizedReturn - riskFreeRate) / (downsideStd * (decimal)Math.Sqrt(252)) : 0m;
            var calmarRatio = maxDrawdown > 0 ? annualizedReturn / maxDrawdown : 0m;

            var periodType = periodName switch
            {
                "1周" => "week",
                "1月" => "month",
                "3月" => "quarter",
                "6月" => "half_year",
                "1年" or "近1年" => "year",
                "近3年" => "three_year",
                _ => "month"
            };

            return new FundPerformance
            {
                Code = navHistory.First().Code,
                PeriodType = periodType,
                PeriodValue = periodName,
                NavGrowthRate = decimal.Round(navGrowthRate, 4),
                MaxDrawdown = decimal.Round(maxDrawdown, 4),
                Volatility = decimal.Round(annualizedVolatility, 4),
                SharpeRatio = decimal.Round(sharpeRatio, 4),
                SortinoRatio = decimal.Round(sortinoRatio, 4),
                CalmarRatio = decimal.Round(calmarRatio, 4),
                AnnualReturn = decimal.Round(annualizedReturn, 4),
                DownsideStd = decimal.Round(downsideStd, 4),
                UpdateTime = DateTime.Now
            };
        }

        private static decimal CalculateStdDev(List<decimal> values)
        {
            if (values.Count < 2)
                return 0m;

            var avg = values.Average();
            var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
            return (decimal)Math.Sqrt((double)(sumOfSquares / (values.Count - 1)));
        }

        private static decimal CalculateMaxDrawdown(List<FundNavHistory> orderedNav)
        {
            decimal maxDrawdown = 0m;
            decimal peak = orderedNav.First().Nav;

            foreach (var nav in orderedNav)
            {
                if (nav.Nav > peak)
                    peak = nav.Nav;

                var drawdown = peak > 0 ? (peak - nav.Nav) / peak : 0m;
                if (drawdown > maxDrawdown)
                    maxDrawdown = drawdown;
            }

            return maxDrawdown;
        }

        private static readonly string[] RecentPeriodTypes = { "成立以来", "今年以来", "近1周", "近1月", "近3月", "近6月", "近1年", "近3年" };

        public async Task UpdateRecentPerformanceAsync(string fundCode)
        {
            try
            {
                var output = ExecutePythonScript("get_performance", fundCode);
                var performanceList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);

                foreach (var item in performanceList)
                {
                    var periodType = item.TryGetValue("periodType", out var periodValue) ? periodValue.ToString() : "month";
                    var periodValueStr = periodType switch
                    {
                        "week" => "1周",
                        "month" => "1月",
                        "quarter" => "3月",
                        "year" => "1年",
                        _ => periodType
                    };

                    if (!RecentPeriodTypes.Contains(periodValueStr))
                    {
                        continue;
                    }

                    var navGrowthRate = item.TryGetValue("navGrowthRate", out var growthValue)
                        ? decimal.Parse(growthValue.ToString().Replace("%", "")) / 100
                        : 0m;
                    var maxDrawdown = item.TryGetValue("maxDrawdown", out var drawdownValue)
                        ? decimal.Parse(drawdownValue.ToString().Replace("%", "")) / 100
                        : (decimal?)null;
                    var sharpeRatio = item.TryGetValue("sharpeRatio", out var sharpeValue)
                        ? decimal.Parse(sharpeValue.ToString())
                        : (decimal?)null;

                    var performance = await _performanceRepository.Query()
                        .FirstOrDefaultAsync(p => p.Code == fundCode && p.PeriodType == periodType && p.PeriodValue == periodValueStr);

                    if (performance == null)
                    {
                        performance = new FundPerformance
                        {
                            Code = fundCode,
                            PeriodType = periodType,
                            PeriodValue = periodValueStr,
                            NavGrowthRate = navGrowthRate,
                            MaxDrawdown = maxDrawdown,
                            SharpeRatio = sharpeRatio,
                            UpdateTime = DateTime.Now
                        };
                        await _performanceRepository.AddAsync(performance);
                    }
                    else
                    {
                        performance.PeriodValue = periodValueStr;
                        performance.NavGrowthRate = navGrowthRate;
                        performance.MaxDrawdown = maxDrawdown;
                        performance.SharpeRatio = sharpeRatio;
                        performance.UpdateTime = DateTime.Now;
                        await _performanceRepository.UpdateAsync(performance);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update recent fund performance: {FundCode}", fundCode);
            }
        }

        // 获取并更新基金经理数据
        // 获取并更新基金经理数据（只插入新记录，不更新已有记录）
        public async Task<List<FundManager>> UpdateFundManagers(string fundCode)
        {
            try
            {
                var output = ExecutePythonScript("get_managers", fundCode);

                if (string.IsNullOrWhiteSpace(output) || output.StartsWith("{"))
                {
                    _logger.LogWarning("Python script returned empty or error for fund {FundCode} managers", fundCode);
                    return new List<FundManager>();
                }

                var managerList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);

                if (managerList == null || managerList.Count == 0)
                {
                    return new List<FundManager>();
                }

                var result = new List<FundManager>();

                foreach (var item in managerList)
                {
                    var managerName = item.TryGetValue("managerName", out var nameValue) ? nameValue.ToString() : "";
                    var startDate = item.TryGetValue("startDate", out var startValue)
                        ? DateOnly.Parse(startValue.ToString())
                        : DateOnly.FromDateTime(DateTime.Now);
                    var endDate = item.TryGetValue("endDate", out var endValue) && endValue.ToString() != ""
                        ? DateOnly.Parse(endValue.ToString())
                        : (DateOnly?)null;
                    var tenure = item.TryGetValue("tenure", out var tenureValue)
                        ? decimal.Parse(tenureValue.ToString())
                        : 0m;

                    var manager = await _managerRepository.Query()
                        .FirstOrDefaultAsync(m => m.Code == fundCode && m.ManagerName == managerName && m.StartDate == startDate);

                    if (manager == null)
                    {
                        manager = new FundManager
                        {
                            Code = fundCode,
                            ManagerName = managerName,
                            StartDate = startDate,
                            EndDate = endDate,
                            Tenure = tenure,
                            ManageDays = (int)(tenure * 365),
                            UpdateTime = DateTime.Now
                        };
                        await _managerRepository.AddAsync(manager);
                        result.Add(manager);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update fund managers: {FundCode}", fundCode);
                return new List<FundManager>();
            }
        }

        // 获取并更新基金资产规模数据（只插入新记录，不更新已有记录）
        public async Task<List<FundAssetScale>> UpdateFundAssetScale(string fundCode, string startDate, string endDate)
        {
            try
            {
                var output = ExecutePythonScript("get_asset_scale", fundCode);

                if (string.IsNullOrWhiteSpace(output) || output.StartsWith("{"))
                {
                    _logger.LogWarning("Python script returned empty or error for fund {FundCode} asset scale", fundCode);
                    return new List<FundAssetScale>();
                }

                var scaleList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);

                if (scaleList == null || scaleList.Count == 0)
                {
                    return new List<FundAssetScale>();
                }

                var result = new List<FundAssetScale>();

                foreach (var item in scaleList)
                {
                    var date = DateOnly.Parse(item.TryGetValue("date", out var dateValue) ? dateValue.ToString() : DateTime.Now.ToString("yyyy-MM-dd"));
                    var assetScale = item.TryGetValue("assetScale", out var assetValue) ? decimal.Parse(assetValue.ToString()) : 0m;
                    var shareScale = item.TryGetValue("shareScale", out var shareValue) ? decimal.Parse(shareValue.ToString()) : 0m;

                    var scale = await _assetScaleRepository.Query()
                        .FirstOrDefaultAsync(s => s.Code == fundCode && s.Date == date);

                    if (scale == null)
                    {
                        scale = new FundAssetScale
                        {
                            Code = fundCode,
                            Date = date,
                            AssetScale = assetScale,
                            ShareScale = shareScale,
                            UpdateTime = DateTime.Now
                        };
                        await _assetScaleRepository.AddAsync(scale);
                        result.Add(scale);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update fund asset scale: {FundCode}", fundCode);
                return new List<FundAssetScale>();
            }
        }

        // 获取并更新基金申购状态数据
        // 获取并更新基金申购状态数据（只插入新记录，不更新已有记录）
        public async Task<List<FundPurchaseStatus>> UpdateFundPurchaseStatus(string fundCode)
        {
            try
            {
                var output = ExecutePythonScript("get_purchase_status", fundCode);

                if (string.IsNullOrWhiteSpace(output) || output.StartsWith("{"))
                {
                    _logger.LogWarning("Python script returned empty or error for fund {FundCode} purchase status", fundCode);
                    return new List<FundPurchaseStatus>();
                }

                var statusList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);

                if (statusList == null || statusList.Count == 0)
                {
                    return new List<FundPurchaseStatus>();
                }

                var result = new List<FundPurchaseStatus>();

                foreach (var item in statusList)
                {
                    if (item == null) continue;

                    string? dateStr = null;
                    if (item.TryGetValue("date", out var dateValue) && dateValue != null)
                    {
                        dateStr = dateValue.ToString();
                    }
                    if (string.IsNullOrEmpty(dateStr)) continue;

                    if (!DateOnly.TryParse(dateStr, out var date)) continue;

                    var purchaseStatusStr = "";
                    if (item.TryGetValue("purchaseStatus", out var statusValue) && statusValue != null)
                    {
                        purchaseStatusStr = statusValue.ToString() ?? "";
                    }

                    decimal? purchaseLimit = null;
                    if (item.TryGetValue("purchaseLimit", out var limitValue) && limitValue != null)
                    {
                        var limitStr = limitValue.ToString();
                        if (!string.IsNullOrEmpty(limitStr) && decimal.TryParse(limitStr, out var lv))
                        {
                            purchaseLimit = lv;
                        }
                    }

                    decimal? purchaseFeeRate = null;
                    if (item.TryGetValue("purchaseFeeRate", out var feeValue) && feeValue != null)
                    {
                        var feeStr = feeValue.ToString()?.Replace("%", "");
                        if (!string.IsNullOrEmpty(feeStr) && decimal.TryParse(feeStr, out var fv))
                        {
                            purchaseFeeRate = fv / 100;
                        }
                    }

                    var status = await _purchaseStatusRepository.Query()
                        .FirstOrDefaultAsync(s => s.Code == fundCode && s.Date == date);

                    if (status == null)
                    {
                        status = new FundPurchaseStatus
                        {
                            Code = fundCode,
                            Date = date,
                            PurchaseStatus = purchaseStatusStr,
                            PurchaseLimit = purchaseLimit,
                            PurchaseFeeRate = purchaseFeeRate,
                            UpdateTime = DateTime.Now
                        };
                        await _purchaseStatusRepository.AddAsync(status);
                        result.Add(status);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update fund purchase status: {FundCode}", fundCode);
                return new List<FundPurchaseStatus>();
            }
        }

        // 获取并更新基金赎回状态数据（只插入新记录，不更新已有记录）
        public async Task<List<FundRedemptionStatus>> UpdateFundRedemptionStatus(string fundCode)
        {
            try
            {
                var output = ExecutePythonScript("get_redemption_status", fundCode);

                if (string.IsNullOrWhiteSpace(output) || output.StartsWith("{"))
                {
                    _logger.LogWarning("Python script returned empty or error for fund {FundCode} redemption status", fundCode);
                    return new List<FundRedemptionStatus>();
                }

                var statusList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);

                if (statusList == null || statusList.Count == 0)
                {
                    return new List<FundRedemptionStatus>();
                }

                var result = new List<FundRedemptionStatus>();

                foreach (var item in statusList)
                {
                    if (item == null) continue;

                    string? dateStr = null;
                    if (item.TryGetValue("date", out var dateValue) && dateValue != null)
                    {
                        dateStr = dateValue.ToString();
                    }
                    if (string.IsNullOrEmpty(dateStr)) continue;

                    if (!DateOnly.TryParse(dateStr, out var date)) continue;

                    var redemptionStatusStr = "";
                    if (item.TryGetValue("redemptionStatus", out var statusValue) && statusValue != null)
                    {
                        redemptionStatusStr = statusValue.ToString() ?? "";
                    }

                    decimal? redemptionLimit = null;
                    if (item.TryGetValue("redemptionLimit", out var limitValue) && limitValue != null)
                    {
                        var limitStr = limitValue.ToString();
                        if (!string.IsNullOrEmpty(limitStr) && decimal.TryParse(limitStr, out var lv))
                        {
                            redemptionLimit = lv;
                        }
                    }

                    decimal? redemptionFeeRate = null;
                    if (item.TryGetValue("redemptionFeeRate", out var feeValue) && feeValue != null)
                    {
                        var feeStr = feeValue.ToString()?.Replace("%", "");
                        if (!string.IsNullOrEmpty(feeStr) && decimal.TryParse(feeStr, out var fv))
                        {
                            redemptionFeeRate = fv / 100;
                        }
                    }

                    var status = await _redemptionStatusRepository.Query()
                        .FirstOrDefaultAsync(s => s.Code == fundCode && s.Date == date);

                    if (status == null)
                    {
                        status = new FundRedemptionStatus
                        {
                            Code = fundCode,
                            Date = date,
                            RedemptionStatus = redemptionStatusStr,
                            RedemptionLimit = redemptionLimit,
                            RedemptionFeeRate = redemptionFeeRate,
                            UpdateTime = DateTime.Now
                        };
                        await _redemptionStatusRepository.AddAsync(status);
                        result.Add(status);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update fund redemption status: {FundCode}", fundCode);
                return new List<FundRedemptionStatus>();
            }
        }

        // 获取并更新基金公司行为数据（只插入新记录，不更新已有记录）
        public async Task<List<FundCorporateActions>> UpdateFundCorporateActions(string fundCode)
        {
            try
            {
                var output = ExecutePythonScript("get_corporate_actions", fundCode);

                if (string.IsNullOrWhiteSpace(output) || output.StartsWith("{"))
                {
                    _logger.LogWarning("Python script returned empty or error for fund {FundCode} corporate actions", fundCode);
                    return new List<FundCorporateActions>();
                }

                var actionsList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(output);

                if (actionsList == null || actionsList.Count == 0)
                {
                    return new List<FundCorporateActions>();
                }

                var result = new List<FundCorporateActions>();

                foreach (var item in actionsList)
                {
                    if (item == null) continue;

                    string? exDateStr = null;
                    if (item.TryGetValue("exDate", out var exDateValue) && exDateValue != null)
                    {
                        exDateStr = exDateValue.ToString();
                    }
                    if (string.IsNullOrEmpty(exDateStr)) continue;

                    DateOnly exDate;
                    if (!DateOnly.TryParse(exDateStr, out exDate)) continue;

                    var eventType = "";
                    if (item.TryGetValue("eventType", out var eventValue) && eventValue != null)
                    {
                        eventType = eventValue.ToString() ?? "";
                    }

                    decimal? dividendPerShare = null;
                    if (item.TryGetValue("dividendPerShare", out var dividendValue) && dividendValue != null)
                    {
                        var dividendStr = dividendValue.ToString();
                        if (!string.IsNullOrEmpty(dividendStr) && decimal.TryParse(dividendStr, out var dv))
                        {
                            dividendPerShare = dv;
                        }
                    }

                    DateOnly? paymentDate = null;
                    if (item.TryGetValue("paymentDate", out var paymentValue) && paymentValue != null)
                    {
                        var paymentDateStr = paymentValue.ToString();
                        if (!string.IsNullOrEmpty(paymentDateStr) && DateOnly.TryParse(paymentDateStr, out var pd))
                        {
                            paymentDate = pd;
                        }
                    }

                    decimal? splitRatio = null;
                    if (item.TryGetValue("splitRatio", out var splitValue) && splitValue != null)
                    {
                        var splitRatioStr = splitValue.ToString();
                        if (!string.IsNullOrEmpty(splitRatioStr) && decimal.TryParse(splitRatioStr, out var sr))
                        {
                            splitRatio = sr;
                        }
                    }

                    DateOnly? recordDate = null;
                    if (item.TryGetValue("recordDate", out var recordValue) && recordValue != null)
                    {
                        var recordDateStr = recordValue.ToString();
                        if (!string.IsNullOrEmpty(recordDateStr) && DateOnly.TryParse(recordDateStr, out var rd))
                        {
                            recordDate = rd;
                        }
                    }

                    var eventDescription = "";
                    if (item.TryGetValue("eventDescription", out var descValue) && descValue != null)
                    {
                        eventDescription = descValue.ToString() ?? "";
                    }

                    DateOnly? announcementDate = null;
                    if (item.TryGetValue("announcementDate", out var announcementValue) && announcementValue != null)
                    {
                        var announcementDateStr = announcementValue.ToString();
                        if (!string.IsNullOrEmpty(announcementDateStr) && DateOnly.TryParse(announcementDateStr, out var ad))
                        {
                            announcementDate = ad;
                        }
                    }

                    var action = await _corporateActionsRepository.Query()
                        .FirstOrDefaultAsync(a => a.Code == fundCode && a.ExDate == exDate && a.EventType == eventType);

                    if (action == null)
                    {
                        action = new FundCorporateActions
                        {
                            Code = fundCode,
                            ExDate = exDate,
                            EventType = eventType,
                            DividendPerShare = dividendPerShare,
                            PaymentDate = paymentDate,
                            SplitRatio = splitRatio,
                            RecordDate = recordDate,
                            EventDescription = eventDescription,
                            AnnouncementDate = announcementDate,
                            UpdateTime = DateTime.Now
                        };
                        await _corporateActionsRepository.AddAsync(action);
                        result.Add(action);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update fund corporate actions: {FundCode}", fundCode);
                return new List<FundCorporateActions>();
            }
        }

        public async Task<bool> HasNewCorporateActionsAsync(string fundCode, string sinceDate)
        {
            try
            {
                var since = DateOnly.Parse(sinceDate);
                var count = await _corporateActionsRepository.Query()
                    .Where(a => a.Code == fundCode && a.ExDate >= since)
                    .CountAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"检查公司行为记录失败: {fundCode}");
                return false;
            }
        }
    }
}
