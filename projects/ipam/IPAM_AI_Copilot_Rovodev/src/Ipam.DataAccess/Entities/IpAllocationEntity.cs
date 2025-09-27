using Azure;
using Azure.Data.Tables;
using Ipam.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Ipam.DataAccess.Entities
{
    /// <summary>
    /// Represents an IP allocation entity in the IPAM system
    /// </summary>
    public class IpAllocationEntity : ITableEntity, IEntity
    {
        /// <summary>
        /// Gets or sets the partition key (AddressSpaceId)
        /// </summary>
        public string PartitionKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the row key (IpId)
        /// </summary>
        public string RowKey { get; set; } = string.Empty;
        
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the IP node
        /// </summary>
        public string Id 
        { 
            get => RowKey; 
            set => RowKey = value; 
        }

        /// <summary>
        /// Gets or sets the address space identifier
        /// </summary>
        public string AddressSpaceId 
        { 
            get => PartitionKey; 
            set => PartitionKey = value; 
        }

        public string Prefix { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        private string _childrenIds = string.Empty;
        public List<string> ChildrenIds
        {
            get => string.IsNullOrEmpty(_childrenIds) ? new List<string>() : 
                JsonSerializer.Deserialize<List<string>>(_childrenIds) ?? new List<string>();
            set => _childrenIds = JsonSerializer.Serialize(value ?? throw new ArgumentNullException(nameof(value)));
        }

        private string _tags = string.Empty;
        public Dictionary<string, string> Tags
        {
            get => string.IsNullOrEmpty(_tags) ? new Dictionary<string, string>() : 
                JsonSerializer.Deserialize<Dictionary<string, string>>(_tags) ?? new Dictionary<string, string>();
            set => _tags = JsonSerializer.Serialize(value ?? throw new ArgumentNullException(nameof(value)));
        }

        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}
