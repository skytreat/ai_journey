using System.ComponentModel.DataAnnotations;

namespace Ipam.DataAccess.Api.Models
{
    /// <summary>
    /// Address Space Data Transfer Object
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    /// <summary>
    /// Create Address Space DTO
    /// </summary>
    public class CreateAddressSpaceDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update Address Space DTO
    /// </summary>
    public class UpdateAddressSpaceDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}