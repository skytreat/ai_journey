using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_corporate_actions")]
    public class FundCorporateActions
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("ex_date")]
        public DateOnly ExDate { get; set; }

        [Column("event_type")]
        [StringLength(20)]
        public string EventType { get; set; }

        [Column("dividend_per_share", TypeName = "DECIMAL(10,4)")]
        public decimal? DividendPerShare { get; set; }

        [Column("payment_date")]
        public DateOnly? PaymentDate { get; set; }

        [Column("split_ratio", TypeName = "DECIMAL(10,4)")]
        public decimal? SplitRatio { get; set; }

        [Column("record_date")]
        public DateOnly? RecordDate { get; set; }

        [Column("event_description")]
        [StringLength(500)]
        public string EventDescription { get; set; }

        [Column("announcement_date")]
        public DateOnly? AnnouncementDate { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}