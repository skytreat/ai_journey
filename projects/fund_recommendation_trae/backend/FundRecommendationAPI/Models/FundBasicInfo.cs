using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_basic_info")]
    public class FundBasicInfo
    {
        [Key]
        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; }

        [Column("fund_type")]
        [StringLength(20)]
        public string FundType { get; set; }

        [Column("share_type")]
        [StringLength(10)]
        public string ShareType { get; set; }

        [Column("main_fund_code")]
        [StringLength(20)]
        public string MainFundCode { get; set; }

        [Column("establish_date")]
        public DateOnly EstablishDate { get; set; }

        [Column("list_date")]
        public DateOnly? ListDate { get; set; }

        [Column("manager")]
        [StringLength(100)]
        public string Manager { get; set; }

        [Column("custodian")]
        [StringLength(100)]
        public string Custodian { get; set; }

        [Column("management_fee_rate")]
        [StringLength(20)]
        public string ManagementFeeRate { get; set; }

        [Column("custodian_fee_rate")]
        [StringLength(20)]
        public string CustodianFeeRate { get; set; }

        [Column("sales_fee_rate")]
        [StringLength(20)]
        public string? SalesFeeRate { get; set; }

        [Column("benchmark")]
        [StringLength(500)]
        public string Benchmark { get; set; }

        [Column("tracking_target")]
        [StringLength(100)]
        public string TrackingTarget { get; set; }

        [Column("investment_style")]
        [StringLength(20)]
        public string InvestmentStyle { get; set; }

        [Column("risk_level")]
        [StringLength(10)]
        public string RiskLevel { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }

        public ICollection<FundNavHistory> NavHistories { get; set; }
        public ICollection<FundPerformance> Performances { get; set; }
    }
}