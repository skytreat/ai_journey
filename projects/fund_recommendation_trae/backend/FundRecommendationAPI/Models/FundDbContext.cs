using Microsoft.EntityFrameworkCore;

namespace FundRecommendationAPI.Models
{
    public class FundDbContext : DbContext
    {
        public FundDbContext(DbContextOptions<FundDbContext> options) : base(options)
        {
        }

        public DbSet<FundBasicInfo> FundBasicInfo { get; set; }
        public DbSet<FundNavHistory> FundNavHistory { get; set; }
        public DbSet<FundPerformance> FundPerformance { get; set; }
        public DbSet<FundAssetScale> FundAssetScale { get; set; }
        public DbSet<FundManager> FundManager { get; set; }
        public DbSet<FundPurchaseStatus> FundPurchaseStatus { get; set; }
        public DbSet<FundRedemptionStatus> FundRedemptionStatus { get; set; }
        public DbSet<FundCorporateActions> FundCorporateActions { get; set; }
        public DbSet<UserFavoriteFunds> UserFavoriteFunds { get; set; }
        public DbSet<UserFavoriteScores> UserFavoriteScores { get; set; }
        public DbSet<SystemUpdateHistory> SystemUpdateHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置复合主键
            modelBuilder.Entity<FundNavHistory>()
                .HasKey(n => new { n.Code, n.Date });

            modelBuilder.Entity<FundAssetScale>()
                .HasKey(s => new { s.Code, s.Date });

            modelBuilder.Entity<FundPurchaseStatus>()
                .HasKey(p => new { p.Code, p.Date });

            modelBuilder.Entity<FundRedemptionStatus>()
                .HasKey(r => new { r.Code, r.Date });

            modelBuilder.Entity<FundManager>()
                .HasKey(m => new { m.Code, m.ManagerName, m.StartDate });

            // 配置索引
            modelBuilder.Entity<FundBasicInfo>()
                .HasIndex(f => f.Code) // 基金代码索引
                .IsUnique();

            modelBuilder.Entity<FundBasicInfo>()
                .HasIndex(f => f.FundType); // 基金类型索引

            modelBuilder.Entity<FundBasicInfo>()
                .HasIndex(f => f.RiskLevel); // 风险等级索引

            modelBuilder.Entity<FundBasicInfo>()
                .HasIndex(f => f.Manager); // 基金管理人索引

            modelBuilder.Entity<FundNavHistory>()
                .HasIndex(n => new { n.Code, n.Date }) // 基金代码和日期复合索引
                .IsUnique();

            modelBuilder.Entity<FundNavHistory>()
                .HasIndex(n => n.Date); // 日期索引

            modelBuilder.Entity<FundPerformance>()
                .HasIndex(p => new { p.Code, p.PeriodType }) // 基金代码和周期类型复合索引
                .IsUnique();

            modelBuilder.Entity<FundPerformance>()
                .HasIndex(p => new { p.PeriodType, p.Code, p.NavGrowthRate }); // 周期类型、基金代码和净值增长率复合索引

            // 新表索引配置
            modelBuilder.Entity<FundAssetScale>()
                .HasIndex(s => new { s.Code, s.Date }) // 基金代码和日期复合索引
                .IsUnique();

            modelBuilder.Entity<FundManager>()
                .HasIndex(m => m.Code); // 基金代码索引

            modelBuilder.Entity<FundPurchaseStatus>()
                .HasIndex(p => new { p.Code, p.Date }) // 基金代码和日期复合索引
                .IsUnique();

            modelBuilder.Entity<FundRedemptionStatus>()
                .HasIndex(r => new { r.Code, r.Date }) // 基金代码和日期复合索引
                .IsUnique();

            modelBuilder.Entity<FundCorporateActions>()
                .HasIndex(c => new { c.Code, c.ExDate }); // 基金代码和除权日期复合索引

            modelBuilder.Entity<FundCorporateActions>()
                .HasIndex(c => c.EventType); // 事件类型索引

            modelBuilder.Entity<UserFavoriteFunds>()
                .HasIndex(u => new { u.UserId, u.FundCode }) // 用户ID和基金代码复合唯一索引
                .IsUnique();

            modelBuilder.Entity<UserFavoriteFunds>()
                .HasIndex(u => u.UserId); // 用户ID索引

            modelBuilder.Entity<UserFavoriteScores>()
                .HasIndex(s => new { s.UserId, s.FundCode, s.ScoreDate }) // 用户ID、基金代码和评分日期复合唯一索引
                .IsUnique();

            modelBuilder.Entity<UserFavoriteScores>()
                .HasIndex(s => s.UserId); // 用户ID索引

            modelBuilder.Entity<SystemUpdateHistory>()
                .HasIndex(s => s.CreatedAt); // 创建时间索引

            modelBuilder.Entity<SystemUpdateHistory>()
                .HasIndex(s => s.Status); // 状态索引

            // 配置关系
            modelBuilder.Entity<FundNavHistory>()
                .HasOne(n => n.Fund)
                .WithMany(f => f.NavHistories)
                .HasForeignKey(n => n.Code);

            modelBuilder.Entity<FundPerformance>()
                .HasOne(p => p.Fund)
                .WithMany(f => f.Performances)
                .HasForeignKey(p => p.Code);
        }
    }
}