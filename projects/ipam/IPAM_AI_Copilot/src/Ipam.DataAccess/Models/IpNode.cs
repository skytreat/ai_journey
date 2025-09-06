using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Ipam.DataAccess.Models
{
    /// <summary>
    /// Represents an IP node entity in the IPAM system
    /// </summary>
    public class IpNode : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Prefix { get; set; }
        public string ParentId { get; set; }
        private string _childrenIds;
        public string[] ChildrenIds
        {
            get => string.IsNullOrEmpty(_childrenIds) ? Array.Empty<string>() : 
                JsonSerializer.Deserialize<string[]>(_childrenIds);
            set => _childrenIds = JsonSerializer.Serialize(value);
        }

        private string _tags;
        public Dictionary<string, string> Tags
        {
            get => string.IsNullOrEmpty(_tags) ? new Dictionary<string, string>() : 
                JsonSerializer.Deserialize<Dictionary<string, string>>(_tags);
            set => _tags = JsonSerializer.Serialize(value);
        }

        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}
