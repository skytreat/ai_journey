using Azure;
using Azure.Data.Tables;
using Ipam.DataAccess.Interfaces;
using System;
using System.Collections.Generic;

namespace Ipam.DataAccess.Models
{
    /// <summary>
    /// Represents an IP address entity in the IPAM system
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IPAddress : ITableEntity, IEntity
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
        /// Gets or sets the tags associated with this IP address
        /// </summary>
        public List<IPAddressTag> Tags { get; set; } = new List<IPAddressTag>();

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
    /// Represents a tag associated with an IP address
    /// </summary>
    public class IPAddressTag
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}