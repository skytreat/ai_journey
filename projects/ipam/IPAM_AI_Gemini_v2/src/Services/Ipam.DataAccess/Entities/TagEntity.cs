
using System;
using Azure;
using Azure.Data.Tables;

namespace Ipam.DataAccess.Entities
{
    public class TagEntity : ITableEntity
    {
        public string Description { get; set; }
        public string Type { get; set; }
        public string KnownValues { get; set; } // JSON string
        public string Attributes { get; set; } // JSON string
        public string Implies { get; set; } // JSON string
        public DateTimeOffset? CreatedOn { get; set; }
        public DateTimeOffset? ModifiedOn { get; set; }

        public TagEntity()
        {
            Description = string.Empty;
            Type = string.Empty;
            KnownValues = string.Empty;
            Attributes = string.Empty;
            Implies = string.Empty;
            PartitionKey = string.Empty;
            RowKey = string.Empty;
        }

        // ITableEntity properties
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
