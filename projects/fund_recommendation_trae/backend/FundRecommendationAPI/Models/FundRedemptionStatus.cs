using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_redemption_status")]
    public class FundRedemptionStatus
    {
        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("date")]
        public DateOnly Date { get; set; }

        [Column("redemption_status")]
        [StringLength(20)]
        public string RedemptionStatus { get; set; }

        [Column("redemption_limit", TypeName = "DECIMAL(12,2)")]
        public decimal? RedemptionLimit { get; set; }

        [Column("redemption_fee_rate", TypeName = "DECIMAL(5,4)")]
        public decimal? RedemptionFeeRate { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}