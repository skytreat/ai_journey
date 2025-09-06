
using System;
using Azure;
using Azure.Data.Tables;

namespace Ipam.DataAccess.Entities
{
    public class IpAddressEntity : ITableEntity
    {
        public string Prefix { get; set; }
        public string Tags { get; set; } // JSON string for directly applied tags (inheritable and non-inheritable)
        public string ParentId { get; set; }
        public DateTimeOffset? CreatedOn { get; set; }
        public DateTimeOffset? ModifiedOn { get; set; }

        public IpAddressEntity()
        {
            Prefix = string.Empty;
            Tags = string.Empty;
            ParentId = string.Empty;
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
