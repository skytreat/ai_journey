using System.Threading.Tasks;
using System.Collections.Generic;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for audit service
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Logs an audit event
        /// </summary>
        Task LogAuditEventAsync(string operation, string entityType, string entityId, string userId, Dictionary<string, object>? changes = null, Dictionary<string, object>? metadata = null);
        
        /// <summary>
        /// Gets audit events for an entity
        /// </summary>
        Task<IEnumerable<object>> GetAuditEventsAsync(string entityType, string entityId);
    }
}