
using System;
using Azure;
using Azure.Data.Tables;

namespace Ipam.DataAccess.Entities
{
    public class UserRoleEntity : ITableEntity
    {
        public string Role { get; set; }

        public UserRoleEntity()
        {
            Role = string.Empty;
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
