using System.ComponentModel.DataAnnotations;

namespace Ipam.DataAccess.Api.Models
{
    /// <summary>
    /// IP Address Data Transfer Object
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IPAddressDto
    {
        public string Id { get; set; } = string.Empty;
        public string AddressSpaceId { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public List<IPAddressTagDto> Tags { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    /// <summary>
    /// IP Address Tag DTO
    /// </summary>
    public class IPAddressTagDto
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Create IP Address DTO
    /// </summary>
    public class CreateIPAddressDto
    {
        [Required]
        [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\/(?:[0-9]|[1-2][0-9]|3[0-2])$|^(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}\/(?:[0-9]|[1-9][0-9]|1[0-1][0-9]|12[0-8])$", 
            ErrorMessage = "Invalid CIDR format")]
        public string Prefix { get; set; } = string.Empty;

        public string? ParentId { get; set; }

        public List<CreateIPAddressTagDto> Tags { get; set; } = new();
    }

    /// <summary>
    /// Create IP Address Tag DTO
    /// </summary>
    public class CreateIPAddressTagDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update IP Address DTO
    /// </summary>
    public class UpdateIPAddressDto
    {
        [Required]
        [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\/(?:[0-9]|[1-2][0-9]|3[0-2])$|^(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}\/(?:[0-9]|[1-9][0-9]|1[0-1][0-9]|12[0-8])$", 
            ErrorMessage = "Invalid CIDR format")]
        public string Prefix { get; set; } = string.Empty;

        public string? ParentId { get; set; }

        public List<UpdateIPAddressTagDto> Tags { get; set; } = new();
    }

    /// <summary>
    /// Update IP Address Tag DTO
    /// </summary>
    public class UpdateIPAddressTagDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Value { get; set; } = string.Empty;
    }
}