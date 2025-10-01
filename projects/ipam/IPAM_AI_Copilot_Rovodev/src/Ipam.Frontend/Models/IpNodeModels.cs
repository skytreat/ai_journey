using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ipam.Frontend.Validation;

namespace Ipam.Frontend.Models
{
    /// <summary>
    /// Model for creating a new IP node
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
        public class IpNodeCreateModel
        {
            /// <summary>
            /// Optional Id for idempotency support
            /// </summary>
            public string? Id { get; set; }

            [Required]
            public string AddressSpaceId { get; set; }

            [Required]
            [CidrValidation]
            [RegularExpression(@"^([0-9]{1,3}\.){3}[0-9]{1,3}\/[0-9]{1,2}$", 
                ErrorMessage = "Invalid CIDR format")]
            public string Prefix { get; set; }

            [TagsValidation]
            public Dictionary<string, string> Tags { get; set; }
        }

    /// <summary>
    /// Model for querying IP nodes
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpNodeQueryModel
    {
        /// <summary>
        /// Gets or sets the address space identifier
        /// </summary>
        [Required(ErrorMessage = "Address space ID is required")]
    public string? AddressSpaceId { get; set; }

        /// <summary>
        /// Gets or sets the IP prefix in CIDR format
        /// </summary>
        [CidrValidation]
    public string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets the tags for filtering
        /// </summary>
        [TagsValidation]
    public Dictionary<string, string>? Tags { get; set; }
    }

    /// <summary>
    /// Model for updating an IP node
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpNodeUpdateModel
    {
        [Required]
        [CidrValidation]
        [RegularExpression(@"^([0-9]{1,3}\.){3}[0-9]{1,3}\/[0-9]{1,2}$", 
            ErrorMessage = "Invalid CIDR format")]
    public string? Prefix { get; set; }

        [TagsValidation]
    public Dictionary<string, string>? Tags { get; set; }
    }

    /// <summary>
    /// Response model for IP node operations
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpNodeResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the IP node
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the address space identifier this IP node belongs to
        /// </summary>
    public string? AddressSpaceId { get; set; }

        /// <summary>
        /// Gets or sets the IP prefix in CIDR format
        /// </summary>
    public string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets the parent node identifier
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// Gets or sets the collection of child node identifiers
        /// </summary>
        public IEnumerable<string> ChildrenIds { get; set; }

        /// <summary>
        /// Gets or sets the IP node tags
        /// </summary>
    public Dictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the last modification timestamp
        /// </summary>
        public DateTime ModifiedOn { get; set; }
    }
}
