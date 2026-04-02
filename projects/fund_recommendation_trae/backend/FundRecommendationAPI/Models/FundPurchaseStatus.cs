using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_purchase_status")]
    public class FundPurchaseStatus
    {
        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("date")]
        public DateOnly Date { get; set; }

        [Column("purchase_status")]
        [StringLength(20)]
        public string PurchaseStatus { get; set; }

        [Column("purchase_limit", TypeName = "DECIMAL(12,2)")]
        public decimal? PurchaseLimit { get; set; }

        [Column("purchase_fee_rate", TypeName = "DECIMAL(5,4)")]
        public decimal? PurchaseFeeRate { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}