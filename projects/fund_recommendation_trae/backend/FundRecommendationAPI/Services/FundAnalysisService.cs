using FundRecommendationAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FundRecommendationAPI.Services
{
    public class FundAnalysisService : IFundAnalysisService
    {
        private readonly IRepository<FundBasicInfo> _fundRepository;
        private readonly IRepository<FundNavHistory> _navHistoryRepository;
        private readonly IRepository<FundPerformance> _performanceRepository;

        private static readonly Dictionary<string, int> PeriodDays = new()
        {
            { "week", 7 },
            { "month", 30 },
            { "quarter", 90 },
            { "year", 365 }
        };

        public FundAnalysisService(
            IRepository<FundBasicInfo> fundRepository,
            IRepository<FundNavHistory> navHistoryRepository,
            IRepository<FundPerformance> performanceRepository)
        {
            _fundRepository = fundRepository ?? throw new ArgumentNullException(nameof(fundRepository));
            _navHistoryRepository = navHistoryRepository ?? throw new ArgumentNullException(nameof(navHistoryRepository));
            _performanceRepository = performanceRepository ?? throw new ArgumentNullException(nameof(performanceRepository));
        }

        public async Task<List<FundRanking>> GetFundRanking(string period = "month", int limit = 10, string order = "desc")
        {
            if (!PeriodDays.TryGetValue(period.ToLower(), out int days))
            {
                days = 30;
            }

            var endDate = DateOnly.FromDateTime(DateTime.Now);
            var startDate = endDate.AddDays(-days);

            var navHistory = await _navHistoryRepository.Query()
                .Where(n => n.Date >= startDate && n.Date <= endDate)
                .ToListAsync();

            var fundReturns = navHistory
                .GroupBy(n => n.Code)
                .Select(g =>
                {
                    var orderedNav = g.OrderBy(n => n.Date).ToList();
                    if (orderedNav.Count < 2)
                    {
                        return new { Code = g.Key, ReturnRate = 0m };
                    }

                    var firstNav = orderedNav.First().Nav;
                    var lastNav = orderedNav.Last().Nav;

                    if (firstNav == 0)
                    {
                        return new { Code = g.Key, ReturnRate = 0m };
                    }

                    var returnRate = (lastNav - firstNav) / firstNav;
                    return new { Code = g.Key, ReturnRate = returnRate };
                })
                .OrderByDescending(x => x.ReturnRate)
                .Take(limit * 2)
                .ToList();

            var fundCodes = fundReturns.Select(x => x.Code).Take(limit).ToList();
            var funds = await _fundRepository.Query()
                .Where(f => fundCodes.Contains(f.Code))
                .ToListAsync();

            var result = new List<FundRanking>();
            int rank = 1;
            foreach (var fundReturn in fundReturns.Take(limit))
            {
                var fund = funds.FirstOrDefault(f => f.Code == fundReturn.Code);
                if (fund != null)
                {
                    result.Add(new FundRanking
                    {
                        Rank = rank++,
                        Code = fund.Code,
                        Name = fund.Name,
                        FundType = fund.FundType,
                        ReturnRate = Math.Round(fundReturn.ReturnRate * 100, 2),
                        Nav = fundReturns.First(x => x.Code == fund.Code).ReturnRate
                    });
                }
            }

            if (order == "asc")
            {
                result = result.OrderBy(r => r.ReturnRate).ToList();
                for (int i = 0; i < result.Count; i++)
                {
                    result[i].Rank = i + 1;
                }
            }

            return result;
        }

        public async Task<List<FundChangeRanking>> GetFundChangeRanking(string period = "month", int limit = 10, string type = "absolute")
        {
            if (!PeriodDays.TryGetValue(period.ToLower(), out int days))
            {
                days = 30;
            }

            var now = DateOnly.FromDateTime(DateTime.Now);
            var currentStart = now.AddDays(-days);
            var previousStart = currentStart.AddDays(-days);

            var currentNav = await _navHistoryRepository.Query()
                .Where(n => n.Date >= currentStart && n.Date <= now)
                .ToListAsync();

            var previousNav = await _navHistoryRepository.Query()
                .Where(n => n.Date >= previousStart && n.Date < currentStart)
                .ToListAsync();

            var fundChanges = currentNav
                .GroupBy(n => n.Code)
                .Select(g =>
                {
                    var currentData = g.OrderBy(n => n.Date).ToList();
                    var previousData = previousNav.Where(n => n.Code == g.Key).OrderBy(n => n.Date).ToList();

                    decimal currentReturn = 0;
                    if (currentData.Count >= 2 && currentData.First().Nav > 0)
                    {
                        currentReturn = (currentData.Last().Nav - currentData.First().Nav) / currentData.First().Nav;
                    }

                    decimal previousReturn = 0;
                    if (previousData.Count >= 2 && previousData.First().Nav > 0)
                    {
                        previousReturn = (previousData.Last().Nav - previousData.First().Nav) / previousData.First().Nav;
                    }

                    var changeValue = currentReturn - previousReturn;

                    decimal changeRate = 0;
                    if (previousReturn > 0)
                    {
                        changeRate = changeValue / previousReturn;
                    }

                    return new
                    {
                        Code = g.Key,
                        CurrentReturn = currentReturn,
                        PreviousReturn = previousReturn,
                        ChangeValue = changeValue,
                        ChangeRate = changeRate
                    };
                })
                .OrderByDescending(x => x.ChangeValue)
                .Take(limit)
                .ToList();

            var fundCodes = fundChanges.Select(x => x.Code).ToList();
            var funds = await _fundRepository.Query()
                .Where(f => fundCodes.Contains(f.Code))
                .ToListAsync();

            var result = new List<FundChangeRanking>();
            int rank = 1;
            foreach (var fc in fundChanges)
            {
                var fund = funds.FirstOrDefault(f => f.Code == fc.Code);
                if (fund != null)
                {
                    result.Add(new FundChangeRanking
                    {
                        Rank = rank++,
                        Code = fund.Code,
                        Name = fund.Name,
                        FundType = fund.FundType,
                        ChangeValue = Math.Round(fc.ChangeValue * 100, 2),
                        ChangeRate = Math.Round(fc.ChangeRate * 100, 2)
                    });
                }
            }

            return result;
        }

        public async Task<List<FundConsistency>> GetFundConsistency(string startDate = "2023-01-01", string endDate = "2024-01-01", int limit = 10)
        {
            if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            {
                start = DateOnly.FromDateTime(DateTime.Now.AddYears(-1));
                end = DateOnly.FromDateTime(DateTime.Now);
            }

            var months = GetMonthRanges(start, end);
            if (months.Count == 0)
            {
                return new List<FundConsistency>();
            }

            var topNFunds = new Dictionary<string, int>();
            const int topN = 20;

            foreach (var (monthStart, monthEnd) in months)
            {
                var monthNav = await _navHistoryRepository.Query()
                    .Where(n => n.Date >= monthStart && n.Date <= monthEnd)
                    .ToListAsync();

                var monthReturns = monthNav
                    .GroupBy(n => n.Code)
                    .Select(g =>
                    {
                        var ordered = g.OrderBy(n => n.Date).ToList();
                        if (ordered.Count < 2 || ordered.First().Nav == 0)
                        {
                            return new { Code = g.Key, Return = 0m };
                        }
                        return new { Code = g.Key, Return = (ordered.Last().Nav - ordered.First().Nav) / ordered.First().Nav };
                    })
                    .OrderByDescending(x => x.Return)
                    .Take(topN)
                    .Select(x => x.Code)
                    .ToList();

                foreach (var code in monthReturns)
                {
                    if (!topNFunds.ContainsKey(code))
                    {
                        topNFunds[code] = 0;
                    }
                    topNFunds[code]++;
                }
            }

            var threshold = (int)Math.Ceiling(months.Count * 0.5);
            var consistentFunds = topNFunds
                .Where(kv => kv.Value >= threshold)
                .OrderByDescending(kv => kv.Value)
                .Take(limit * 2)
                .ToList();

            var fundCodes = consistentFunds.Select(x => x.Key).ToList();
            var funds = await _fundRepository.Query()
                .Where(f => fundCodes.Contains(f.Code))
                .ToListAsync();

            var result = new List<FundConsistency>();
            foreach (var cf in consistentFunds.Take(limit))
            {
                var fund = funds.FirstOrDefault(f => f.Code == cf.Key);
                if (fund != null)
                {
                    var consistencyScore = (decimal)cf.Value / months.Count * 100;
                    var avgReturn = await CalculateAverageReturn(fund.Code, start, end);

                    result.Add(new FundConsistency
                    {
                        Code = fund.Code,
                        Name = fund.Name,
                        FundType = fund.FundType,
                        ConsistencyScore = Math.Round(consistencyScore, 2),
                        AverageReturn = Math.Round(avgReturn * 100, 2)
                    });
                }
            }

            return result;
        }

        public async Task<List<FundMultiFactorScore>> GetFundMultiFactorScore(int limit = 10, string[] factors = null)
        {
            var performanceData = await _performanceRepository.Query()
                .Where(p => p.PeriodType == "成立以来")
                .Take(limit * 3)
                .ToListAsync();

            var fundCodes = performanceData.Select(p => p.Code).Distinct().ToList();
            var funds = await _fundRepository.Query()
                .Where(f => fundCodes.Contains(f.Code))
                .ToListAsync();

            var scoredFunds = new List<FundMultiFactorScore>();

            foreach (var code in fundCodes)
            {
                var perf = performanceData.FirstOrDefault(p => p.Code == code);
                var fund = funds.FirstOrDefault(f => f.Code == code);

                if (perf == null || fund == null) continue;

                var scores = CalculateFactorScores(perf);
                var totalScore = scores.ReturnScore * 0.35m +
                                 scores.RiskScore * 0.30m +
                                 scores.RiskAdjustedReturnScore * 0.25m +
                                 scores.RankingScore * 0.10m;

                scoredFunds.Add(new FundMultiFactorScore
                {
                    Code = code,
                    Name = fund.Name,
                    FundType = fund.FundType,
                    TotalScore = Math.Round(totalScore, 2),
                    Scores = scores
                });
            }

            return scoredFunds
                .OrderByDescending(f => f.TotalScore)
                .Take(limit)
                .ToList();
        }

        private FactorScores CalculateFactorScores(FundPerformance perf)
        {
            const decimal maxReturn = 2.0m;
            const decimal maxDrawdown = 0.5m;
            const decimal maxVolatility = 0.5m;
            const decimal maxSharpe = 3.0m;

            var returnScore = Math.Min(perf.NavGrowthRate / maxReturn * 100m, 100m);
            var riskScore = Math.Max(100m - ((perf.MaxDrawdown ?? 0m) / maxDrawdown * 100m), 0m);
            var volatilityScore = Math.Max(100m - ((perf.Volatility ?? 0m) / maxVolatility * 100m), 0m);
            var riskScoreCombined = (riskScore + volatilityScore) / 2m;

            var sharpeScore = Math.Min(((perf.SharpeRatio ?? 0m) / maxSharpe) * 100m, 100m);

            decimal rankingScore = 0m;
            if (perf.TotalInCategory.HasValue && perf.TotalInCategory.Value > 0)
            {
                rankingScore = (1m - (decimal)perf.RankInCategory / perf.TotalInCategory.Value) * 100m;
            }

            return new FactorScores
            {
                ReturnScore = decimal.Round(Math.Max(0m, returnScore), 2),
                RiskScore = decimal.Round(Math.Max(0m, riskScoreCombined), 2),
                RiskAdjustedReturnScore = decimal.Round(Math.Max(0m, sharpeScore), 2),
                RankingScore = decimal.Round(Math.Max(0m, rankingScore), 2)
            };
        }

        private async Task<decimal> CalculateAverageReturn(string code, DateOnly startDate, DateOnly endDate)
        {
            var navHistory = await _navHistoryRepository.Query()
                .Where(n => n.Code == code && n.Date >= startDate && n.Date <= endDate)
                .OrderBy(n => n.Date)
                .ToListAsync();

            if (navHistory.Count < 2)
            {
                return 0;
            }

            var totalReturn = 0m;
            for (int i = 1; i < navHistory.Count; i++)
            {
                if (navHistory[i - 1].Nav > 0)
                {
                    totalReturn += (navHistory[i].Nav - navHistory[i - 1].Nav) / navHistory[i - 1].Nav;
                }
            }

            return totalReturn / navHistory.Count;
        }

        private List<(DateOnly Start, DateOnly End)> GetMonthRanges(DateOnly start, DateOnly end)
        {
            var ranges = new List<(DateOnly, DateOnly)>();
            var current = new DateOnly(start.Year, start.Month, 1);

            while (current < end)
            {
                var monthEnd = current.AddMonths(1).AddDays(-1);
                if (monthEnd > end)
                {
                    monthEnd = end;
                }

                if (current >= start)
                {
                    ranges.Add((current, monthEnd));
                }

                current = current.AddMonths(1);
            }

            return ranges;
        }

        public async Task<List<FundComparison>> CompareFunds(string[] fundIds)
        {
            var comparisons = new List<FundComparison>();

            if (fundIds == null || fundIds.Length == 0)
            {
                return comparisons;
            }

            var funds = await _fundRepository.Query()
                .Where(f => fundIds.Contains(f.Code))
                .ToListAsync();

            var latestNav = await _navHistoryRepository.Query()
                .Where(n => fundIds.Contains(n.Code))
                .GroupBy(n => n.Code)
                .Select(g => g.OrderByDescending(n => n.Date).First())
                .ToListAsync();

            var performances = await _performanceRepository.Query()
                .Where(p => fundIds.Contains(p.Code))
                .ToListAsync();

            foreach (var fundId in fundIds)
            {
                var fund = funds.FirstOrDefault(f => f.Code == fundId);
                var nav = latestNav.FirstOrDefault(n => n.Code == fundId);
                var perf = performances.FirstOrDefault(p => p.Code == fundId);

                if (fund != null)
                {
                    comparisons.Add(new FundComparison
                    {
                        FundId = fund.Code,
                        FundName = fund.Name,
                        FundType = fund.FundType,
                        Nav = nav?.Nav ?? 0,
                        AccumulatedNav = nav?.AccumulatedNav ?? 0,
                        MonthlyReturn = (perf?.NavGrowthRate ?? 0) / 12,
                        QuarterlyReturn = (perf?.NavGrowthRate ?? 0) / 4,
                        YearlyReturn = perf?.NavGrowthRate ?? 0,
                        MaxDrawdown = perf?.MaxDrawdown ?? 0,
                        SharpeRatio = perf?.SharpeRatio ?? 0
                    });
                }
            }

            return comparisons;
        }

        public decimal CalculateAdjustedNav(decimal nav, decimal accumulatedNav)
        {
            if (accumulatedNav > 0 && nav > 0)
            {
                return accumulatedNav;
            }
            return nav;
        }
    }

    public class FundRanking
    {
        public int Rank { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FundType { get; set; } = string.Empty;
        public decimal ReturnRate { get; set; }
        public decimal Nav { get; set; }
    }

    public class FundChangeRanking
    {
        public int Rank { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FundType { get; set; } = string.Empty;
        public decimal ChangeValue { get; set; }
        public decimal ChangeRate { get; set; }
    }

    public class FundConsistency
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FundType { get; set; } = string.Empty;
        public decimal ConsistencyScore { get; set; }
        public decimal AverageReturn { get; set; }
    }

    public class FactorScores
    {
        public decimal ReturnScore { get; set; }
        public decimal RiskScore { get; set; }
        public decimal RiskAdjustedReturnScore { get; set; }
        public decimal RankingScore { get; set; }
    }

    public class FundMultiFactorScore
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FundType { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public FactorScores Scores { get; set; } = new();
    }

    public class FundComparison
    {
        public string FundId { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string FundType { get; set; } = string.Empty;
        public decimal Nav { get; set; }
        public decimal AccumulatedNav { get; set; }
        public decimal MonthlyReturn { get; set; }
        public decimal QuarterlyReturn { get; set; }
        public decimal YearlyReturn { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal SharpeRatio { get; set; }
    }
}
