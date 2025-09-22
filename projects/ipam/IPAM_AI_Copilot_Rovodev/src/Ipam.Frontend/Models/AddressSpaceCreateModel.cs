using System.ComponentModel.DataAnnotations;

namespace Ipam.Frontend.Models
{
    /// <summary>
    /// Model for creating a new address space
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceCreateModel
    {
        /// <summary>
        /// Gets or sets the ID of the address space.
        /// If not provided, a new ID will be generated.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the address space
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the address space
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}