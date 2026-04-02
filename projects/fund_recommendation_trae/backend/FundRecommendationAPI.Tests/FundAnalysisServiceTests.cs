using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FundRecommendationAPI.Tests;

public class FundAnalysisServiceTests : IAsyncLifetime
{
    private FundAnalysisService _service;
    private FundDbContext _dbContext;
    private IRepository<FundBasicInfo> _fundBasicInfoRepository;
    private IRepository<FundNavHistory> _fundNavHistoryRepository;
    private IRepository<FundPerformance> _fundPerformanceRepository;

    public FundAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<FundDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb" + Guid.NewGuid())
            .Options;

        _dbContext = new FundDbContext(options);
        
        _fundBasicInfoRepository = new Repository<FundBasicInfo>(_dbContext);
        _fundNavHistoryRepository = new Repository<FundNavHistory>(_dbContext);
        _fundPerformanceRepository = new Repository<FundPerformance>(_dbContext);
        
        _service = new FundAnalysisService(_fundBasicInfoRepository, _fundNavHistoryRepository, _fundPerformanceRepository);
    }

    public async Task InitializeAsync()
    {
        // 初始化测试数据
        InitializeTestData();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // 清理资源
        _dbContext.Dispose();
        await Task.CompletedTask;
    }

    private void InitializeTestData()
    {
        // 添加测试基金基本信息
        var funds = new List<FundBasicInfo>
        {
            new FundBasicInfo
            {
                Code = "000001",
                Name = "华夏成长",
                FundType = "混合型",
                ShareType = "A类",
                MainFundCode = "000001",
                Manager = "张三",
                Custodian = "工商银行",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                RiskLevel = "中风险",
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "成长型"
            },
            new FundBasicInfo
            {
                Code = "000002",
                Name = "易方达蓝筹精选",
                FundType = "混合型",
                ShareType = "A类",
                MainFundCode = "000002",
                Manager = "李四",
                Custodian = "建设银行",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
                RiskLevel = "中高风险",
                Benchmark = "中证500指数",
                TrackingTarget = "",
                InvestmentStyle = "价值型"
            },
            new FundBasicInfo
            {
                Code = "000003",
                Name = "嘉实成长收益",
                FundType = "股票型",
                ShareType = "A类",
                MainFundCode = "000003",
                Manager = "王五",
                Custodian = "农业银行",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-4)),
                RiskLevel = "高风险",
                Benchmark = "创业板指数",
                TrackingTarget = "",
                InvestmentStyle = "平衡型"
            }
        };

        _dbContext.FundBasicInfo.AddRange(funds);
        _dbContext.SaveChanges();

        // 添加测试基金净值历史
        var navHistories = new List<FundNavHistory>();
        for (int i = 0; i < 30; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(-i));
            navHistories.AddRange(new[]
            {
                new FundNavHistory
                {
                    Code = "000001",
                    Date = date,
                    Nav = 1.0m + i * 0.01m,
                    AccumulatedNav = 1.0m + i * 0.01m,
                    DailyGrowthRate = 0.001m,
                    UpdateTime = DateTime.Now
                },
                new FundNavHistory
                {
                    Code = "000002",
                    Date = date,
                    Nav = 1.2m + i * 0.015m,
                    AccumulatedNav = 1.2m + i * 0.015m,
                    DailyGrowthRate = 0.0015m,
                    UpdateTime = DateTime.Now
                },
                new FundNavHistory
                {
                    Code = "000003",
                    Date = date,
                    Nav = 1.5m + i * 0.02m,
                    AccumulatedNav = 1.5m + i * 0.02m,
                    DailyGrowthRate = 0.002m,
                    UpdateTime = DateTime.Now
                }
            });
        }

        _dbContext.FundNavHistory.AddRange(navHistories);
        _dbContext.SaveChanges();

        // 添加测试基金业绩指标
        var performances = new List<FundPerformance>
        {
            new FundPerformance { Code = "000001", PeriodType = "1个月", PeriodValue = "30", NavGrowthRate = 0.05m, MaxDrawdown = 0.02m, SharpeRatio = 1.2m },
            new FundPerformance { Code = "000001", PeriodType = "3个月", PeriodValue = "90", NavGrowthRate = 0.15m, MaxDrawdown = 0.05m, SharpeRatio = 1.1m },
            new FundPerformance { Code = "000001", PeriodType = "6个月", PeriodValue = "180", NavGrowthRate = 0.25m, MaxDrawdown = 0.08m, SharpeRatio = 1.0m },
            new FundPerformance { Code = "000001", PeriodType = "1年", PeriodValue = "365", NavGrowthRate = 0.40m, MaxDrawdown = 0.12m, SharpeRatio = 0.9m },
            new FundPerformance { Code = "000002", PeriodType = "1个月", PeriodValue = "30", NavGrowthRate = 0.06m, MaxDrawdown = 0.03m, SharpeRatio = 1.3m },
            new FundPerformance { Code = "000002", PeriodType = "3个月", PeriodValue = "90", NavGrowthRate = 0.18m, MaxDrawdown = 0.06m, SharpeRatio = 1.2m },
            new FundPerformance { Code = "000002", PeriodType = "6个月", PeriodValue = "180", NavGrowthRate = 0.30m, MaxDrawdown = 0.09m, SharpeRatio = 1.1m },
            new FundPerformance { Code = "000002", PeriodType = "1年", PeriodValue = "365", NavGrowthRate = 0.45m, MaxDrawdown = 0.15m, SharpeRatio = 1.0m },
            new FundPerformance { Code = "000003", PeriodType = "1个月", PeriodValue = "30", NavGrowthRate = 0.08m, MaxDrawdown = 0.04m, SharpeRatio = 1.4m },
            new FundPerformance { Code = "000003", PeriodType = "3个月", PeriodValue = "90", NavGrowthRate = 0.20m, MaxDrawdown = 0.07m, SharpeRatio = 1.3m },
            new FundPerformance { Code = "000003", PeriodType = "6个月", PeriodValue = "180", NavGrowthRate = 0.35m, MaxDrawdown = 0.10m, SharpeRatio = 1.2m },
            new FundPerformance { Code = "000003", PeriodType = "1年", PeriodValue = "365", NavGrowthRate = 0.50m, MaxDrawdown = 0.18m, SharpeRatio = 1.1m }
        };

        _dbContext.FundPerformance.AddRange(performances);
        _dbContext.SaveChanges();

        // 添加测试基金经理信息
        var managers = new List<FundManager>
        {
            new FundManager { Code = "000001", ManagerName = "张三", Tenure = 3.5m, StartDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)), EndDate = null },
            new FundManager { Code = "000001", ManagerName = "李四", Tenure = 2.0m, StartDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)), EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)) },
            new FundManager { Code = "000002", ManagerName = "王五", Tenure = 2.5m, StartDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)), EndDate = null },
            new FundManager { Code = "000003", ManagerName = "赵六", Tenure = 3.0m, StartDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-4)), EndDate = null }
        };

        _dbContext.FundManager.AddRange(managers);
        _dbContext.SaveChanges();

        // 添加测试基金规模
        var scales = new List<FundAssetScale>();
        for (int i = 0; i < 12; i++)
        {
            var date = DateOnly.FromDateTime(DateTime.Now.AddMonths(-i));
            scales.AddRange(new[]
            {
                new FundAssetScale { Code = "000001", Date = date, AssetScale = 10000 + i * 1000, ShareScale = 9000 + i * 900, UpdateTime = DateTime.Now },
                new FundAssetScale { Code = "000002", Date = date, AssetScale = 15000 + i * 1500, ShareScale = 13500 + i * 1350, UpdateTime = DateTime.Now },
                new FundAssetScale { Code = "000003", Date = date, AssetScale = 20000 + i * 2000, ShareScale = 18000 + i * 1800, UpdateTime = DateTime.Now }
            });
        }

        _dbContext.FundAssetScale.AddRange(scales);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetFundRanking_ReturnsRankingList()
    {
        // 测试单周期基金排名
        var rankings = await _service.GetFundRanking("month", 10, "desc");
        
        Assert.NotNull(rankings);
        Assert.NotEmpty(rankings);
        Assert.True(rankings.Count <= 10);
        Assert.Equal(1, rankings.First().Rank);
    }

    [Fact]
    public async Task GetFundChangeRanking_ReturnsChangeRankingList()
    {
        // 测试周期变化率排名
        var rankings = await _service.GetFundChangeRanking("month", 10, "absolute");
        
        Assert.NotNull(rankings);
        Assert.NotEmpty(rankings);
        Assert.True(rankings.Count <= 10);
        Assert.Equal(1, rankings.First().Rank);
    }

    [Fact]
    public async Task GetFundConsistency_ReturnsConsistencyList()
    {
        // 测试多周期一致性筛选
        var funds = await _service.GetFundConsistency("2023-01-01", "2024-01-01", 10);
        
        Assert.NotNull(funds);
        Assert.NotEmpty(funds);
        Assert.True(funds.Count <= 10);
        // 验证一致性得分按降序排列
        var firstScore = funds.First().ConsistencyScore;
        var lastScore = funds.Last().ConsistencyScore;
        Assert.True(firstScore >= lastScore);
    }

    [Fact]
    public async Task GetFundMultiFactorScore_ReturnsMultiFactorScoreList()
    {
        // 测试多因子量化评估
        var funds = await _service.GetFundMultiFactorScore(10, new[] { "return", "risk", "riskAdjustedReturn", "ranking" });
        
        Assert.NotNull(funds);
        Assert.NotEmpty(funds);
        Assert.True(funds.Count <= 10);
        // 验证总得分按降序排列
        var firstScore = funds.First().TotalScore;
        var lastScore = funds.Last().TotalScore;
        Assert.True(firstScore >= lastScore);
    }

    [Fact]
    public async Task CompareFunds_ReturnsComparisonList()
    {
        // 测试基金对比分析
        var funds = await _service.CompareFunds(new[] { "000001", "000002", "000003" });
        
        Assert.NotNull(funds);
        Assert.NotEmpty(funds);
        Assert.Equal(3, funds.Count);
        Assert.Contains(funds, f => f.FundId == "000001");
        Assert.Contains(funds, f => f.FundId == "000002");
        Assert.Contains(funds, f => f.FundId == "000003");
    }

    [Fact]
        public void CalculateAdjustedNav_ReturnsCorrectValue()
        {
            // 测试复权净值计算
            var adjustedNav = _service.CalculateAdjustedNav(1.0m, 1.5m);
            Assert.Equal(1.5m, adjustedNav);

            adjustedNav = _service.CalculateAdjustedNav(1.0m, 0);
            Assert.Equal(1.0m, adjustedNav);

            adjustedNav = _service.CalculateAdjustedNav(0, 1.5m);
            Assert.Equal(1.5m, adjustedNav);

            adjustedNav = _service.CalculateAdjustedNav(0, 0);
            Assert.Equal(0, adjustedNav);
        }

        [Fact]
        public async Task GetFundRanking_ReturnsAscendingRanking()
        {
            // 测试单周期基金排名（升序）
            var rankings = await _service.GetFundRanking("month", 10, "asc");
            
            Assert.NotNull(rankings);
            Assert.NotEmpty(rankings);
            Assert.True(rankings.Count <= 10);
            // 验证排名按升序排列
            var firstRate = rankings.First().ReturnRate;
            var lastRate = rankings.Last().ReturnRate;
            Assert.True(firstRate <= lastRate);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_ReturnsWithNullFactors()
        {
            // 测试多因子量化评估（null factors参数）
            var funds = await _service.GetFundMultiFactorScore(10, null);
            
            Assert.NotNull(funds);
            Assert.NotEmpty(funds);
            Assert.True(funds.Count <= 10);
        }

        [Fact]
        public async Task CompareFunds_ReturnsEmptyList_WhenEmptyArray()
        {
            // 测试基金对比分析（空数组）
            var funds = await _service.CompareFunds(new string[] { });
            
            Assert.NotNull(funds);
            Assert.Empty(funds);
        }

        [Fact]
        public async Task CompareFunds_ReturnsOnlyExistingFunds()
        {
            // 测试基金对比分析（包含不存在的基金）
            var funds = await _service.CompareFunds(new[] { "000001", "999999" });
            
            Assert.NotNull(funds);
            Assert.NotEmpty(funds);
            Assert.Single(funds);
            Assert.Equal("000001", funds.First().FundId);
        }

        [Fact]
        public void FundAnalysisService_ShouldHandleNullContext()
        {
            Assert.Throws<ArgumentNullException>(() => new FundAnalysisService(null, null, null));
        }

        [Fact]
        public async Task GetFundRanking_ShouldHandleInvalidPeriod()
        {
            // 测试无效的周期参数
            var rankings = await _service.GetFundRanking("invalid", 10, "desc");
            
            Assert.NotNull(rankings);
            Assert.NotEmpty(rankings);
        }

        [Fact]
        public async Task GetFundRanking_ShouldHandleZeroLimit()
        {
            // 测试limit=0的情况
            var rankings = await _service.GetFundRanking("month", 0, "desc");
            
            Assert.NotNull(rankings);
            Assert.Empty(rankings);
        }

        [Fact]
        public async Task GetFundRanking_ShouldHandleNegativeLimit()
        {
            // 测试负limit值的情况
            var rankings = await _service.GetFundRanking("month", -10, "desc");
            
            Assert.NotNull(rankings);
            Assert.NotEmpty(rankings);
        }

        [Fact]
        public async Task GetFundChangeRanking_ShouldHandleInvalidPeriod()
        {
            // 测试无效的周期参数
            var rankings = await _service.GetFundChangeRanking("invalid", 10, "absolute");
            
            Assert.NotNull(rankings);
            Assert.NotEmpty(rankings);
        }

        [Fact]
        public async Task GetFundChangeRanking_ShouldHandleInvalidType()
        {
            // 测试无效的类型参数
            var rankings = await _service.GetFundChangeRanking("month", 10, "invalid");
            
            Assert.NotNull(rankings);
            Assert.NotEmpty(rankings);
        }

        [Fact]
        public async Task GetFundConsistency_ShouldHandleInvalidDateRange()
        {
            // 测试无效的日期范围
            var funds = await _service.GetFundConsistency("invalid", "invalid", 10);
            
            Assert.NotNull(funds);
            Assert.NotEmpty(funds);
        }

        [Fact]
        public async Task GetFundConsistency_ShouldHandleStartDateAfterEndDate()
        {
            // 测试开始日期晚于结束日期的情况
            var funds = await _service.GetFundConsistency("2024-01-01", "2023-01-01", 10);
            
            Assert.NotNull(funds);
            Assert.NotEmpty(funds);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_ShouldHandleEmptyFactors()
        {
            // 测试空因子数组
            var funds = await _service.GetFundMultiFactorScore(10, new string[] { });
            
            Assert.NotNull(funds);
            Assert.NotEmpty(funds);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_ShouldHandleInvalidFactors()
        {
            // 测试无效的因子
            var funds = await _service.GetFundMultiFactorScore(10, new[] { "invalid", "factor" });
            
            Assert.NotNull(funds);
            Assert.NotEmpty(funds);
        }

        [Fact]
        public async Task CompareFunds_ShouldHandleNullArray()
        {
            // 测试null基金代码数组
            var funds = await _service.CompareFunds(null);
            
            Assert.NotNull(funds);
            Assert.Empty(funds);
        }

        [Fact]
        public async Task CompareFunds_ShouldHandleNonExistentFunds()
        {
            // 测试包含多个不存在的基金
            var funds = await _service.CompareFunds(new[] { "999999", "888888", "777777" });
            
            Assert.NotNull(funds);
            Assert.Empty(funds);
        }

        [Fact]
        public void CalculateAdjustedNav_ShouldHandleNegativeValues()
        {
            // 测试负值的情况
            var adjustedNav = _service.CalculateAdjustedNav(-1.0m, 1.5m);
            Assert.Equal(1.5m, adjustedNav);

            adjustedNav = _service.CalculateAdjustedNav(1.0m, -1.5m);
            Assert.Equal(-0.5m, adjustedNav);
        }

        [Fact]
        public async Task GetFundRanking_ShouldHandleLargeLimit()
        {
            // 测试大limit值的情况
            var rankings = await _service.GetFundRanking("month", 100, "desc");
            
            Assert.NotNull(rankings);
            Assert.NotEmpty(rankings);
            Assert.True(rankings.Count <= 100);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_ShouldHandleLargeLimit()
        {
            // 测试大limit值的情况
            var funds = await _service.GetFundMultiFactorScore(100, new[] { "return", "risk" });
            
            Assert.NotNull(funds);
            Assert.NotEmpty(funds);
            Assert.True(funds.Count <= 100);
        }
}
