using System;
using System.ComponentModel.DataAnnotations;

namespace Ipam.Frontend.Models
{
    /// <summary>
    /// Model for querying address spaces
    /// </summary>
    public class AddressSpaceQueryModel
    {
        /// <summary>
        /// Optional name filter for address spaces
        /// </summary>
        public string NameFilter { get; set; }

        /// <summary>
        /// Optional creation date filter
        /// </summary>
        public DateTime? CreatedAfter { get; set; }
    }

    /// <summary>
    /// Model for creating or updating an address space
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpaceModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
    }
}
    /// <summary>
    /// Model for updating an existing address space
    /// </summary>
    public class AddressSpaceUpdateModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
    }
}
