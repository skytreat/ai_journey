using Azure;
using Azure.Data.Tables;
using Ipam.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Ipam.DataAccess.Entities
{
    /// <summary>
    /// Represents a tag entity in the IPAM system
    /// </summary>
    public class TagEntity : ITableEntity, IEntity
    {
        /// <summary>
        /// Gets or sets the partition key (AddressSpaceId)
        /// </summary>
        public string PartitionKey { get; set; }
        
        /// <summary>
        /// Gets or sets the row key (TagName)
        /// </summary>
        public string RowKey { get; set; }
        
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>
        /// Gets or sets the address space identifier
        /// </summary>
        public string AddressSpaceId 
        { 
            get => PartitionKey; 
            set => PartitionKey = value; 
        }

        /// <summary>
        /// Gets or sets the tag name
        /// </summary>
        public string Name 
        { 
            get => RowKey; 
            set => RowKey = value; 
        }

        public string Description { get; set; }
        public string Type { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }

        private string _knownValues;
        public List<string> KnownValues
        {
            get => string.IsNullOrEmpty(_knownValues) ? new List<string>() : 
                JsonSerializer.Deserialize<List<string>>(_knownValues);
            set => _knownValues = JsonSerializer.Serialize(value);
        }

        private string _implies;
        
        /// <summary>
        /// Gets or sets the tag implications
        /// </summary>
        /// <remarks>
        /// The format is:
        /// {
        ///     "ImpliedTag1": {
        ///         "CurrentTagValue1": "ImpliedTag1Value1",
        ///         "CurrentTagValue2": "ImpliedTag1Value2"
        ///     },
        ///     "ImpliedTag2": {
        ///         "CurrentTagValue1": "ImpliedTag2Value1"
        ///     }
        /// }
        /// </remarks>
        public Dictionary<string, Dictionary<string, string>> Implies
        {
            get => string.IsNullOrEmpty(_implies) ? new Dictionary<string, Dictionary<string, string>>() :
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(_implies);
            set => _implies = JsonSerializer.Serialize(value);
        }

        private string _attributes;
        
        /// <summary>
        /// Gets or sets additional attributes for the tag
        /// <remarks>
        /// The format is:
        /// {
        ///     "Attribute1": {
        ///         "CurrentTagValue1": "Attribute1Value1",
        ///         "CurrentTagValue2": "Attribute1Value2"
        ///     },
        ///     "Attribute2": {
        ///         "CurrentTagValue1": "Attribute2Value1"
        ///     }
        /// }
        /// </remarks>
        public Dictionary<string, Dictionary<string, string>> Attributes
        {
            get => string.IsNullOrEmpty(_attributes) ? new Dictionary<string, Dictionary<string, string>>() :
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(_attributes);
            set => _attributes = JsonSerializer.Serialize(value);
        }
    }
}