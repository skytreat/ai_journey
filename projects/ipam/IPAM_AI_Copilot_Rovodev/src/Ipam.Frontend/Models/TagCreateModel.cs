using System.ComponentModel.DataAnnotations;

namespace Ipam.Frontend.Models
{
    /// <summary>
    /// Model for creating a new tag
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class TagCreateModel
    {
        /// <summary>
        /// Gets or sets the address space identifier
        /// </summary>
        [Required]
        public string AddressSpaceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the tag
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the tag
        /// </summary>
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the tag (Inheritable or NonInheritable)
        /// </summary>
        [Required]
        [RegularExpression("^(Inheritable|NonInheritable)$", ErrorMessage = "Type must be either 'Inheritable' or 'NonInheritable'")]
        public string Type { get; set; } = "NonInheritable";

        /// <summary>
        /// Gets or sets the known values for the tag (optional)
        /// </summary>
        public List<string> KnownValues { get; set; }

        /// <summary>
        /// Gets or sets the tag implications (for inheritable tags)
        /// </summary>
        public Dictionary<string, Dictionary<string, string>>? Implies { get; set; }

        /// <summary>
        /// Gets or sets the tag attributes
        /// </summary>
        public Dictionary<string, Dictionary<string, string>>? Attributes { get; set; }
    }
}