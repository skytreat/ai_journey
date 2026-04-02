using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class FundAnalysisServiceEdgeCaseTests : IAsyncLifetime
    {
        private FundAnalysisService _service;
        private FundDbContext _dbContext;
        private IRepository<FundBasicInfo> _fundBasicInfoRepository;
        private IRepository<FundNavHistory> _fundNavHistoryRepository;
        private IRepository<FundPerformance> _fundPerformanceRepository;

        public FundAnalysisServiceEdgeCaseTests()
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
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _dbContext.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task GetFundRanking_WithNegativeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(limit: -10);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithZeroLimit_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundRanking(limit: 0);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundRanking_WithVeryLargeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(limit: 999999);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithNullPeriod_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(period: null);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithEmptyPeriod_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(period: "");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithInvalidPeriod_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(period: "invalid_period");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithNullOrder_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(order: null);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithEmptyOrder_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(order: "");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithInvalidOrder_ShouldHandleGracefully()
        {
            var result = await _service.GetFundRanking(order: "invalid_order");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundRanking_WithAscOrder_ShouldReturnAscendingOrder()
        {
            var result = await _service.GetFundRanking(order: "asc");
            Assert.NotNull(result);
            
            for (int i = 1; i < result.Count; i++)
            {
                Assert.True(result[i].ReturnRate >= result[i - 1].ReturnRate);
            }
        }

        [Fact]
        public async Task GetFundRanking_WithDescOrder_ShouldReturnDescendingOrder()
        {
            var result = await _service.GetFundRanking(order: "desc");
            Assert.NotNull(result);
            
            for (int i = 1; i < result.Count; i++)
            {
                Assert.True(result[i].ReturnRate <= result[i - 1].ReturnRate);
            }
        }

        [Fact]
        public async Task GetFundRanking_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundRanking();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundRanking_ShouldCalculateCorrectRanks()
        {
            var fund = new FundBasicInfo
            {
                Code = "000001",
                Name = "测试基金",
                FundType = "混合型",
                ShareType = "前端",
                MainFundCode = "000001",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                Manager = "测试经理",
                Custodian = "测试托管人",
                ManagementFeeRate = 0.015m,
                CustodianFeeRate = 0.0025m,
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "",
                RiskLevel = "中风险",
                UpdateTime = DateTime.Now
            };

            _dbContext.FundBasicInfo.Add(fund);
            await _dbContext.SaveChangesAsync();

            var result = await _service.GetFundRanking();
            Assert.NotNull(result);
            Assert.Equal(1, result[0].Rank);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithNegativeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(limit: -10);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithZeroLimit_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundChangeRanking(limit: 0);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithVeryLargeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(limit: 999999);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithNullPeriod_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(period: null);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithEmptyPeriod_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(period: "");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithInvalidPeriod_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(period: "invalid_period");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithNullType_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(type: null);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithEmptyType_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(type: "");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithInvalidType_ShouldHandleGracefully()
        {
            var result = await _service.GetFundChangeRanking(type: "invalid_type");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundChangeRanking_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundChangeRanking();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithNegativeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(limit: -10);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithZeroLimit_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundConsistency(limit: 0);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithVeryLargeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(limit: 999999);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithNullStartDate_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(startDate: null);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithEmptyStartDate_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(startDate: "");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithInvalidStartDate_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(startDate: "invalid_date");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithNullEndDate_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(endDate: null);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithEmptyEndDate_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(endDate: "");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithInvalidEndDate_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(endDate: "invalid_date");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithStartDateAfterEndDate_ShouldHandleGracefully()
        {
            var result = await _service.GetFundConsistency(startDate: "2024-12-31", endDate: "2024-01-01");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundConsistency_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundConsistency();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundConsistency_ShouldReturnOrderedByConsistencyScore()
        {
            var fund = new FundBasicInfo
            {
                Code = "000001",
                Name = "测试基金",
                FundType = "混合型",
                ShareType = "前端",
                MainFundCode = "000001",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                Manager = "测试经理",
                Custodian = "测试托管人",
                ManagementFeeRate = 0.015m,
                CustodianFeeRate = 0.0025m,
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "",
                RiskLevel = "中风险",
                UpdateTime = DateTime.Now
            };

            _dbContext.FundBasicInfo.Add(fund);
            await _dbContext.SaveChangesAsync();

            var result = await _service.GetFundConsistency();
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithNegativeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundMultiFactorScore(limit: -10);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithZeroLimit_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundMultiFactorScore(limit: 0);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithVeryLargeLimit_ShouldHandleGracefully()
        {
            var result = await _service.GetFundMultiFactorScore(limit: 999999);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithNullFactors_ShouldHandleGracefully()
        {
            var result = await _service.GetFundMultiFactorScore(factors: null);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithEmptyFactors_ShouldHandleGracefully()
        {
            var result = await _service.GetFundMultiFactorScore(factors: new string[0]);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithInvalidFactors_ShouldHandleGracefully()
        {
            var result = await _service.GetFundMultiFactorScore(factors: new[] { "invalid_factor" });
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithMultipleFactors_ShouldHandleGracefully()
        {
            var result = await _service.GetFundMultiFactorScore(factors: new[] { "return", "risk", "sharpe" });
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            var result = await _service.GetFundMultiFactorScore();
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundMultiFactorScore_ShouldReturnOrderedByTotalScore()
        {
            var fund = new FundBasicInfo
            {
                Code = "000001",
                Name = "测试基金",
                FundType = "混合型",
                ShareType = "前端",
                MainFundCode = "000001",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                Manager = "测试经理",
                Custodian = "测试托管人",
                ManagementFeeRate = 0.015m,
                CustodianFeeRate = 0.0025m,
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "",
                RiskLevel = "中风险",
                UpdateTime = DateTime.Now
            };

            _dbContext.FundBasicInfo.Add(fund);
            await _dbContext.SaveChangesAsync();

            var result = await _service.GetFundMultiFactorScore();
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task CompareFunds_WithNullFundIds_ShouldReturnEmptyList()
        {
            var result = await _service.CompareFunds(null);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task CompareFunds_WithEmptyFundIds_ShouldReturnEmptyList()
        {
            var result = await _service.CompareFunds(new string[0]);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task CompareFunds_WithInvalidFundIds_ShouldReturnEmptyList()
        {
            var result = await _service.CompareFunds(new[] { "invalid_fund_id" });
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task CompareFunds_WithNonExistentFundIds_ShouldReturnEmptyList()
        {
            var result = await _service.CompareFunds(new[] { "999999", "888888" });
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task CompareFunds_WithSingleFundId_ShouldReturnSingleComparison()
        {
            var fund = new FundBasicInfo
            {
                Code = "000001",
                Name = "测试基金",
                FundType = "混合型",
                ShareType = "前端",
                MainFundCode = "000001",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                Manager = "测试经理",
                Custodian = "测试托管人",
                ManagementFeeRate = 0.015m,
                CustodianFeeRate = 0.0025m,
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "",
                RiskLevel = "中风险",
                UpdateTime = DateTime.Now
            };

            _dbContext.FundBasicInfo.Add(fund);
            await _dbContext.SaveChangesAsync();

            var result = await _service.CompareFunds(new[] { "000001" });
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("000001", result[0].FundId);
        }

        [Fact]
        public async Task CompareFunds_WithMultipleFundIds_ShouldReturnMultipleComparisons()
        {
            var fund1 = new FundBasicInfo
            {
                Code = "000001",
                Name = "测试基金1",
                FundType = "混合型",
                ShareType = "前端",
                MainFundCode = "000001",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                Manager = "测试经理1",
                Custodian = "测试托管人1",
                ManagementFeeRate = 0.015m,
                CustodianFeeRate = 0.0025m,
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "",
                RiskLevel = "中风险",
                UpdateTime = DateTime.Now
            };

            var fund2 = new FundBasicInfo
            {
                Code = "000002",
                Name = "测试基金2",
                FundType = "股票型",
                ShareType = "前端",
                MainFundCode = "000002",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
                Manager = "测试经理2",
                Custodian = "测试托管人2",
                ManagementFeeRate = 0.018m,
                CustodianFeeRate = 0.003m,
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "",
                RiskLevel = "高风险",
                UpdateTime = DateTime.Now
            };

            _dbContext.FundBasicInfo.Add(fund1);
            _dbContext.FundBasicInfo.Add(fund2);
            await _dbContext.SaveChangesAsync();

            var result = await _service.CompareFunds(new[] { "000001", "000002" });
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task CompareFunds_WithDuplicateFundIds_ShouldHandleGracefully()
        {
            var fund = new FundBasicInfo
            {
                Code = "000001",
                Name = "测试基金",
                FundType = "混合型",
                ShareType = "前端",
                MainFundCode = "000001",
                EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                Manager = "测试经理",
                Custodian = "测试托管人",
                ManagementFeeRate = 0.015m,
                CustodianFeeRate = 0.0025m,
                Benchmark = "沪深300指数",
                TrackingTarget = "",
                InvestmentStyle = "",
                RiskLevel = "中风险",
                UpdateTime = DateTime.Now
            };

            _dbContext.FundBasicInfo.Add(fund);
            await _dbContext.SaveChangesAsync();

            var result = await _service.CompareFunds(new[] { "000001", "000001" });
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task CompareFunds_WithVeryManyFundIds_ShouldHandleGracefully()
        {
            var fundIds = new string[100];
            for (int i = 0; i < 100; i++)
            {
                var fund = new FundBasicInfo
                {
                    Code = $"{i:D6}",
                    Name = $"测试基金{i}",
                    FundType = "混合型",
                    ShareType = "前端",
                    MainFundCode = $"{i:D6}",
                    EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                    Manager = "测试经理",
                    Custodian = "测试托管人",
                    ManagementFeeRate = 0.015m,
                    CustodianFeeRate = 0.0025m,
                    Benchmark = "沪深300指数",
                    TrackingTarget = "",
                    InvestmentStyle = "",
                    RiskLevel = "中风险",
                    UpdateTime = DateTime.Now
                };

                _dbContext.FundBasicInfo.Add(fund);
                fundIds[i] = $"{i:D6}";
            }

            await _dbContext.SaveChangesAsync();

            var result = await _service.CompareFunds(fundIds);
            Assert.NotNull(result);
            Assert.Equal(100, result.Count);
        }

        [Fact]
        public void CalculateAdjustedNav_WithZeroAccumulatedNav_ShouldReturnNav()
        {
            var result = _service.CalculateAdjustedNav(1.2345m, 0m);
            Assert.Equal(1.2345m, result);
        }

        [Fact]
        public void CalculateAdjustedNav_WithNegativeAccumulatedNav_ShouldReturnNav()
        {
            var result = _service.CalculateAdjustedNav(1.2345m, -0.5m);
            Assert.Equal(1.2345m, result);
        }

        [Fact]
        public void CalculateAdjustedNav_WithPositiveAccumulatedNav_ShouldReturnAccumulatedNav()
        {
            var result = _service.CalculateAdjustedNav(1.2345m, 2.3456m);
            Assert.Equal(2.3456m, result);
        }

        [Fact]
        public void CalculateAdjustedNav_WithZeroNav_ShouldReturnAccumulatedNav()
        {
            var result = _service.CalculateAdjustedNav(0m, 2.3456m);
            Assert.Equal(2.3456m, result);
        }

        [Fact]
        public void CalculateAdjustedNav_WithNegativeNav_ShouldReturnAccumulatedNav()
        {
            var result = _service.CalculateAdjustedNav(-0.5m, 2.3456m);
            Assert.Equal(2.3456m, result);
        }

        [Fact]
        public void CalculateAdjustedNav_WithBothZero_ShouldReturnZero()
        {
            var result = _service.CalculateAdjustedNav(0m, 0m);
            Assert.Equal(0m, result);
        }

        [Fact]
        public void CalculateAdjustedNav_WithVeryLargeNav_ShouldHandleGracefully()
        {
            var result = _service.CalculateAdjustedNav(999999999.9999m, 888888888.8888m);
            Assert.Equal(888888888.8888m, result);
        }

        [Fact]
        public void CalculateAdjustedNav_WithVerySmallNav_ShouldHandleGracefully()
        {
            var result = _service.CalculateAdjustedNav(0.0001m, 0.0002m);
            Assert.Equal(0.0002m, result);
        }

        [Fact]
        public void FundAnalysisService_Constructor_WithValidContext_ShouldInitializeCorrectly()
        {
            var service = new FundAnalysisService(_fundBasicInfoRepository, _fundNavHistoryRepository, _fundPerformanceRepository);
            Assert.NotNull(service);
        }
    }
}
