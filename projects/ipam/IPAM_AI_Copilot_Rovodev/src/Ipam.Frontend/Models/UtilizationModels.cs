using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ipam.Frontend.Validation;

namespace Ipam.Frontend.Models
{
    /// <summary>
    /// Request model for subnet validation
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class SubnetValidationRequest
    {
        /// <summary>
        /// Gets or sets the proposed CIDR to validate
        /// </summary>
        [Required(ErrorMessage = "Proposed CIDR is required")]
        [CidrValidation]
        [RegularExpression(@"^([0-9]{1,3}\.){3}[0-9]{1,3}\/[0-9]{1,2}$", 
            ErrorMessage = "Invalid CIDR format")]
        public string ProposedCidr { get; set; }
    }

    /// <summary>
    /// Request model for subnet allocation
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class SubnetAllocationRequest
    {
        /// <summary>
        /// Gets or sets the size of the subnet to allocate
        /// </summary>
        [Required(ErrorMessage = "Subnet size is required")]
        [Range(1, 32, ErrorMessage = "Subnet size must be between 1 and 32")]
        public int SubnetSize { get; set; }

        /// <summary>
        /// Gets or sets the tags to apply to the allocated subnet
        /// </summary>
        [TagsValidation]
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }
}