using FundRecommendationAPI.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class DatabaseTests
    {
        private DbContextOptions<FundDbContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<FundDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public void FundDbContext_ShouldCreateAllTables()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                // Assert
                var model = context.Model;
                var entityTypes = model.GetEntityTypes();
                var entityNames = entityTypes.Select(e => e.Name).ToList();

                Assert.Contains(entityNames, name => name.Contains("FundBasicInfo"));
                Assert.Contains(entityNames, name => name.Contains("FundNavHistory"));
                Assert.Contains(entityNames, name => name.Contains("FundPerformance"));
                Assert.Contains(entityNames, name => name.Contains("FundAssetScale"));
                Assert.Contains(entityNames, name => name.Contains("FundManager"));
                Assert.Contains(entityNames, name => name.Contains("FundPurchaseStatus"));
                Assert.Contains(entityNames, name => name.Contains("FundRedemptionStatus"));
                Assert.Contains(entityNames, name => name.Contains("FundCorporateActions"));
                Assert.Contains(entityNames, name => name.Contains("UserFavoriteFunds"));
                Assert.Contains(entityNames, name => name.Contains("UserFavoriteScores"));
            }
        }

        [Fact]
        public void FundBasicInfo_ShouldHaveCorrectProperties()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var fund = new FundBasicInfo
                {
                    Code = "000001",
                    Name = "华夏成长混合",
                    FundType = "混合型",
                    ShareType = "前端",
                    MainFundCode = "000001",
                    EstablishDate = DateOnly.Parse("2001-12-18"),
                    Manager = "巩怀志",
                    Custodian = "中国建设银行",
                    ManagementFeeRate = 0.015m,
                    CustodianFeeRate = 0.0025m,
                    Benchmark = "沪深300指数",
                    TrackingTarget = "",
                    InvestmentStyle = "成长型",
                    RiskLevel = "中风险",
                    UpdateTime = DateTime.Now
                };

                context.FundBasicInfo.Add(fund);
                context.SaveChanges();

                // Assert
                var savedFund = context.FundBasicInfo.FirstOrDefault(f => f.Code == "000001");
                Assert.NotNull(savedFund);
                Assert.Equal("华夏成长混合", savedFund.Name);
                Assert.Equal("混合型", savedFund.FundType);
            }
        }

        [Fact]
        public void FundNavHistory_ShouldHaveCompositePrimaryKey()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var fund = new FundBasicInfo
                {
                    Code = "000001",
                    Name = "华夏成长混合",
                    FundType = "混合型",
                    ShareType = "前端",
                    MainFundCode = "000001",
                    EstablishDate = DateOnly.Parse("2001-12-18"),
                    Manager = "巩怀志",
                    Custodian = "中国建设银行",
                    ManagementFeeRate = 0.015m,
                    CustodianFeeRate = 0.0025m,
                    Benchmark = "沪深300指数",
                    TrackingTarget = "",
                    InvestmentStyle = "成长型",
                    RiskLevel = "中风险",
                    UpdateTime = DateTime.Now
                };

                context.FundBasicInfo.Add(fund);

                var navHistory = new FundNavHistory
                {
                    Code = "000001",
                    Date = DateOnly.Parse("2024-01-01"),
                    Nav = 1.2345m,
                    AccumulatedNav = 3.4567m,
                    DailyGrowthRate = 0.005m,
                    UpdateTime = DateTime.Now
                };

                context.FundNavHistory.Add(navHistory);
                context.SaveChanges();

                // Assert
                var savedNav = context.FundNavHistory.FirstOrDefault(n => n.Code == "000001" && n.Date == DateOnly.Parse("2024-01-01"));
                Assert.NotNull(savedNav);
                Assert.Equal(1.2345m, savedNav.Nav);
                Assert.Equal(3.4567m, savedNav.AccumulatedNav);
            }
        }

        [Fact]
        public void FundPerformance_ShouldHaveCorrectProperties()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var fund = new FundBasicInfo
                {
                    Code = "000001",
                    Name = "华夏成长混合",
                    FundType = "混合型",
                    ShareType = "前端",
                    MainFundCode = "000001",
                    EstablishDate = DateOnly.Parse("2001-12-18"),
                    Manager = "巩怀志",
                    Custodian = "中国建设银行",
                    ManagementFeeRate = 0.015m,
                    CustodianFeeRate = 0.0025m,
                    Benchmark = "沪深300指数",
                    TrackingTarget = "",
                    InvestmentStyle = "成长型",
                    RiskLevel = "中风险",
                    UpdateTime = DateTime.Now
                };

                context.FundBasicInfo.Add(fund);

                var performance = new FundPerformance
                {
                    Code = "000001",
                    PeriodType = "1年",
                    PeriodValue = "2023",
                    NavGrowthRate = 0.15m,
                    MaxDrawdown = 0.2m,
                    SharpeRatio = 1.2m,
                    UpdateTime = DateTime.Now
                };

                context.FundPerformance.Add(performance);
                context.SaveChanges();

                // Assert
                var savedPerformance = context.FundPerformance.FirstOrDefault(p => p.Code == "000001" && p.PeriodType == "1年");
                Assert.NotNull(savedPerformance);
                Assert.Equal(0.15m, savedPerformance.NavGrowthRate);
                Assert.Equal(0.2m, savedPerformance.MaxDrawdown);
            }
        }

        [Fact]
        public void FundAssetScale_ShouldHaveCompositePrimaryKey()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var assetScale = new FundAssetScale
                {
                    Code = "000001",
                    Date = DateOnly.Parse("2024-01-01"),
                    AssetScale = 1000000000m,
                    ShareScale = 800000000m,
                    UpdateTime = DateTime.Now
                };

                context.FundAssetScale.Add(assetScale);
                context.SaveChanges();

                // Assert
                var savedAssetScale = context.FundAssetScale.FirstOrDefault(s => s.Code == "000001" && s.Date == DateOnly.Parse("2024-01-01"));
                Assert.NotNull(savedAssetScale);
                Assert.Equal(1000000000m, savedAssetScale.AssetScale);
                Assert.Equal(800000000m, savedAssetScale.ShareScale);
            }
        }

        [Fact]
        public void FundManager_ShouldHaveCompositePrimaryKey()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var manager = new FundManager
                {
                    Code = "000001",
                    ManagerName = "巩怀志",
                    Tenure = 4.0m,
                    StartDate = DateOnly.Parse("2020-01-01"),
                    EndDate = null,
                    ManageDays = 1460,
                    UpdateTime = DateTime.Now
                };

                context.FundManager.Add(manager);
                context.SaveChanges();

                // Assert
                var savedManager = context.FundManager.FirstOrDefault(m => m.Code == "000001" && m.ManagerName == "巩怀志" && m.StartDate == DateOnly.Parse("2020-01-01"));
                Assert.NotNull(savedManager);
                Assert.Equal(1460, savedManager.ManageDays);
            }
        }

        [Fact]
        public void UserFavoriteFunds_ShouldHaveCorrectProperties()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var favorite = new UserFavoriteFunds
                {
                    UserId = "user1",
                    FundCode = "000001",
                    AddTime = DateTime.Now,
                    SortOrder = 1,
                    Note = "我的第一个自选基金",
                    GroupTag = "核心配置",
                    AlertSettings = "{\"priceAlert\": true}",
                    UpdateTime = DateTime.Now
                };

                context.UserFavoriteFunds.Add(favorite);
                context.SaveChanges();

                // Assert
                var savedFavorite = context.UserFavoriteFunds.FirstOrDefault(f => f.UserId == "user1" && f.FundCode == "000001");
                Assert.NotNull(savedFavorite);
                Assert.Equal("核心配置", savedFavorite.GroupTag);
                Assert.Equal("我的第一个自选基金", savedFavorite.Note);
            }
        }

        [Fact]
        public void UserFavoriteScores_ShouldHaveCorrectProperties()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var score = new UserFavoriteScores
                {
                    UserId = "user1",
                    FundCode = "000001",
                    ScoreDate = DateOnly.Parse("2024-01-01"),
                    TotalScore = 8.5m,
                    ReturnScore = 9.0m,
                    RiskScore = 8.0m,
                    RiskAdjustedReturnScore = 8.5m,
                    RankScore = 8.0m,
                    ScoreChange = 0.5m,
                    ScoreTrend = "上升",
                    WeightConfig = "{\"return\": 0.3, \"risk\": 0.2}",
                    CalculateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                context.UserFavoriteScores.Add(score);
                context.SaveChanges();

                // Assert
                var savedScore = context.UserFavoriteScores.FirstOrDefault(s => s.UserId == "user1" && s.FundCode == "000001" && s.ScoreDate == DateOnly.Parse("2024-01-01"));
                Assert.NotNull(savedScore);
                Assert.Equal(8.5m, savedScore.TotalScore);
                Assert.Equal("上升", savedScore.ScoreTrend);
            }
        }

        [Fact]
        public void FundPurchaseStatus_ShouldHaveCorrectProperties()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var purchaseStatus = new FundPurchaseStatus
                {
                    Code = "000001",
                    Date = DateOnly.Parse("2024-01-01"),
                    PurchaseStatus = "开放",
                    PurchaseLimit = 1000000m,
                    PurchaseFeeRate = 0.015m,
                    UpdateTime = DateTime.Now
                };

                context.FundPurchaseStatus.Add(purchaseStatus);
                context.SaveChanges();

                // Assert
                var savedStatus = context.FundPurchaseStatus.FirstOrDefault(s => s.Code == "000001" && s.Date == DateOnly.Parse("2024-01-01"));
                Assert.NotNull(savedStatus);
                Assert.Equal("开放", savedStatus.PurchaseStatus);
                Assert.Equal(1000000m, savedStatus.PurchaseLimit);
            }
        }

        [Fact]
        public void FundRedemptionStatus_ShouldHaveCorrectProperties()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var redemptionStatus = new FundRedemptionStatus
                {
                    Code = "000001",
                    Date = DateOnly.Parse("2024-01-01"),
                    RedemptionStatus = "开放",
                    RedemptionLimit = 1000000m,
                    RedemptionFeeRate = 0.005m,
                    UpdateTime = DateTime.Now
                };

                context.FundRedemptionStatus.Add(redemptionStatus);
                context.SaveChanges();

                // Assert
                var savedStatus = context.FundRedemptionStatus.FirstOrDefault(s => s.Code == "000001" && s.Date == DateOnly.Parse("2024-01-01"));
                Assert.NotNull(savedStatus);
                Assert.Equal("开放", savedStatus.RedemptionStatus);
                Assert.Equal(0.005m, savedStatus.RedemptionFeeRate);
            }
        }

        [Fact]
        public void FundCorporateActions_ShouldHaveCorrectProperties()
        {
            // Arrange
            var options = CreateInMemoryOptions();

            // Act
            using (var context = new FundDbContext(options))
            {
                context.Database.EnsureCreated();

                var corporateAction = new FundCorporateActions
                {
                    Code = "000001",
                    ExDate = DateOnly.Parse("2024-01-01"),
                    EventType = "分红",
                    DividendPerShare = 0.05m,
                    PaymentDate = DateOnly.Parse("2024-01-05"),
                    EventDescription = "每10份派0.5元",
                    AnnouncementDate = DateOnly.Parse("2023-12-20"),
                    UpdateTime = DateTime.Now
                };

                context.FundCorporateActions.Add(corporateAction);
                context.SaveChanges();

                // Assert
                var savedAction = context.FundCorporateActions.FirstOrDefault(a => a.Code == "000001" && a.EventType == "分红");
                Assert.NotNull(savedAction);
                Assert.Equal("每10份派0.5元", savedAction.EventDescription);
            }
        }
    }
}
