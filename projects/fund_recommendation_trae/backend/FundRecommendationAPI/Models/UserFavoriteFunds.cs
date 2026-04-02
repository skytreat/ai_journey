using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("user_favorite_funds")]
    public class UserFavoriteFunds
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

        [Column("add_time")]
        public DateTime AddTime { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("note")]
        [StringLength(500)]
        public string Note { get; set; }

        [Column("group_tag")]
        [StringLength(50)]
        public string GroupTag { get; set; }

        [Column("alert_settings")]
        [StringLength(200)]
        public string AlertSettings { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}