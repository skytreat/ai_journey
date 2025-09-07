using Azure;
using Azure.Data.Tables;
using Ipam.DataAccess.Interfaces;
using System;
using System.Collections.Generic;

namespace Ipam.DataAccess.Models
{
    /// <summary>
    /// Represents an IP allocation entity in the IPAM system
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpAllocation : ITableEntity, IEntity
    {
        /// <summary>
        /// Gets or sets the partition key of the entity
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the row key of the entity
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the entity
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the ETag of the entity
        /// </summary>
        public ETag ETag { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the IP address
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the address space identifier
        /// </summary>
        public string AddressSpaceId { get; set; }

        /// <summary>
        /// Gets or sets the IP prefix in CIDR notation
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the parent IP node identifier
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with this IP allocation
        /// </summary>
        public List<IpAllocationTag> Tags { get; set; } = new List<IpAllocationTag>();

        /// <summary>
        /// Gets or sets the creation date of the IP address
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the last modified date of the IP address
        /// </summary>
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets the status of the IP address
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets additional metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents a tag associated with an IP allocation
    /// </summary>
    public class IpAllocationTag
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}