using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Ipam.DataAccess.Models
{
    /// <summary>
    /// Represents a tag entity in the IPAM system
    /// </summary>
    public class Tag : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Description { get; set; }
        public string Type { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }

        private string _knownValues;
        public string[] KnownValues
        {
            get => string.IsNullOrEmpty(_knownValues) ? Array.Empty<string>() : 
                JsonSerializer.Deserialize<string[]>(_knownValues);
            set => _knownValues = JsonSerializer.Serialize(value);
        }

        private string _implies;
        public Dictionary<string, Dictionary<string, string>> Implies
        {
            get => string.IsNullOrEmpty(_implies) ? new Dictionary<string, Dictionary<string, string>>() : 
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(_implies);
            set => _implies = JsonSerializer.Serialize(value);
        }

        private string _attributes;
        public Dictionary<string, Dictionary<string, string>> Attributes
        {
            get => string.IsNullOrEmpty(_attributes) ? new Dictionary<string, Dictionary<string, string>>() : 
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(_attributes);
            set => _attributes = JsonSerializer.Serialize(value);
        }
    }
}
