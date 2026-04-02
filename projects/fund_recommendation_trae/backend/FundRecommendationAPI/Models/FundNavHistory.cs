using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_nav_history")]
    public class FundNavHistory
    {
        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("date")]
        public DateOnly Date { get; set; }

        [Column("nav", TypeName = "DECIMAL(10,4)")]
        public decimal Nav { get; set; }

        [Column("accumulated_nav", TypeName = "DECIMAL(10,4)")]
        public decimal AccumulatedNav { get; set; }

        [Column("daily_growth_rate", TypeName = "DECIMAL(8,4)")]
        public decimal? DailyGrowthRate { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }

        [ForeignKey("Code")]
        public FundBasicInfo Fund { get; set; }
    }
}