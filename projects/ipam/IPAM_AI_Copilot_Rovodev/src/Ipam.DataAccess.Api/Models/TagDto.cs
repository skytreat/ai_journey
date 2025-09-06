using System.ComponentModel.DataAnnotations;
using Ipam.DataAccess.Models;

namespace Ipam.DataAccess.Api.Models
{
    /// <summary>
    /// Tag Data Transfer Object
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TagType Type { get; set; }
        public List<string> KnownValues { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    /// <summary>
    /// Create Tag DTO
    /// </summary>
    public class CreateTagDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public TagType Type { get; set; }

        public List<string> KnownValues { get; set; } = new();

        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();

        public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new();
    }

    /// <summary>
    /// Update Tag DTO
    /// </summary>
    public class UpdateTagDto
    {
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public TagType Type { get; set; }

        public List<string> KnownValues { get; set; } = new();

        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();

        public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new();
    }
}