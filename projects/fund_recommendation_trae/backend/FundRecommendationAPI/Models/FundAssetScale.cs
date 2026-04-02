using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_asset_scale")]
    public class FundAssetScale
    {
        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("date")]
        public DateOnly Date { get; set; }

        [Column("asset_scale", TypeName = "DECIMAL(12,2)")]
        public decimal AssetScale { get; set; }

        [Column("share_scale", TypeName = "DECIMAL(12,2)")]
        public decimal ShareScale { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}