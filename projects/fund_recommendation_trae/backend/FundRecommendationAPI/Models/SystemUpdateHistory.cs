using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundRecommendationAPI.Models
{
    public class SystemUpdateHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; }

        public int RecordsUpdated { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
