using System;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Base interface for all IPAM entities
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public interface IEntity
    {
        DateTime CreatedOn { get; set; }
        DateTime ModifiedOn { get; set; }
    }
}
