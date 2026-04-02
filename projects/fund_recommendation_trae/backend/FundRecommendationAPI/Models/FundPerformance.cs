using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_performance")]
    public class FundPerformance
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("period_type")]
        [StringLength(20)]
        public string PeriodType { get; set; }

        [Column("period_value")]
        [StringLength(20)]
        public string PeriodValue { get; set; }

        [Column("nav_growth_rate", TypeName = "DECIMAL(8,4)")]
        public decimal NavGrowthRate { get; set; }

        [Column("max_drawdown", TypeName = "DECIMAL(8,4)")]
        public decimal? MaxDrawdown { get; set; }

        [Column("downside_std", TypeName = "DECIMAL(8,4)")]
        public decimal? DownsideStd { get; set; }

        [Column("sharpe_ratio", TypeName = "DECIMAL(8,4)")]
        public decimal? SharpeRatio { get; set; }

        [Column("sortino_ratio", TypeName = "DECIMAL(8,4)")]
        public decimal? SortinoRatio { get; set; }

        [Column("calmar_ratio", TypeName = "DECIMAL(8,4)")]
        public decimal? CalmarRatio { get; set; }

        [Column("annual_return", TypeName = "DECIMAL(8,4)")]
        public decimal? AnnualReturn { get; set; }

        [Column("volatility", TypeName = "DECIMAL(8,4)")]
        public decimal? Volatility { get; set; }

        [Column("rank_in_category")]
        public int? RankInCategory { get; set; }

        [Column("total_in_category")]
        public int? TotalInCategory { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }

        [ForeignKey("Code")]
        public FundBasicInfo Fund { get; set; }
    }
}