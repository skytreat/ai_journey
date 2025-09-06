using Azure;
using Azure.Data.Tables;
using Ipam.DataAccess.Interfaces;
using System;

namespace Ipam.DataAccess.Models
{
    /// <summary>
    /// Represents an IP address space entity in the IPAM system
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class AddressSpace : ITableEntity, IEntity
    {
        /// <summary>
        /// Gets or sets the partition key of the entity (PartitionId)
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the row key of the entity (AddressSpaceId)
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the address space
        /// </summary>
        public string Id 
        { 
            get => RowKey; 
            set => RowKey = value; 
        }

        /// <summary>
        /// Gets or sets the timestamp of the entity
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the ETag of the entity
        /// </summary>
        public ETag ETag { get; set; }

        /// <summary>
        /// Gets or sets the name of the address space
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the address space
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the address space
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the last modified date of the address space
        /// </summary>
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets the status of the address space
        /// </summary>
        public string Status { get; set; }
    }
}
