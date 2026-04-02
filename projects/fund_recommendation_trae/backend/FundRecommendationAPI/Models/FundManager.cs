using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    [Table("fund_manager")]
    public class FundManager
    {
        [Column("code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Column("manager_name")]
        [StringLength(50)]
        public string ManagerName { get; set; }

        [Column("start_date")]
        public DateOnly StartDate { get; set; }

        [Column("end_date")]
        public DateOnly? EndDate { get; set; }

        [Column("tenure", TypeName = "DECIMAL(5,1)")]
        public decimal Tenure { get; set; }

        [Column("manage_days")]
        public int ManageDays { get; set; }

        [Column("update_time")]
        public DateTime UpdateTime { get; set; }
    }
}