using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("user_favorite_scores")]
    public class UserFavoriteScores
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("user_id")]
        [StringLength(50)]
        public string UserId { get; set; }

        [Column("fund_code")]
        [StringLength(20)]
        public string FundCode { get; set; }

        [Column("score_date")]
        public DateOnly ScoreDate { get; set; }

        [Column("total_score", TypeName = "DECIMAL(5,2)")]
        public decimal TotalScore { get; set; }

        [Column("return_score", TypeName = "DECIMAL(5,2)")]
        public decimal ReturnScore { get; set; }

        [Column("risk_score", TypeName = "DECIMAL(5,2)")]
        public decimal RiskScore { get; set; }

        [Column("risk_adjusted_return_score", TypeName = "DECIMAL(5,2)")]
        public decimal RiskAdjustedReturnScore { get; set; }

        [Column("rank_score", TypeName = "DECIMAL(5,2)")]
        public decimal RankScore { get; set; }

        [Column("score_change", TypeName = "DECIMAL(5,2)")]
        public decimal? ScoreChange { get; set; }

        [Column("score_trend")]
        [StringLength(10)]
        public string ScoreTrend { get; set; }

        [Column("weight_config")]
        [StringLength(500)]
        public string WeightConfig { get; set; }

        [Column("calculate_time")]
        public DateTime CalculateTime { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}