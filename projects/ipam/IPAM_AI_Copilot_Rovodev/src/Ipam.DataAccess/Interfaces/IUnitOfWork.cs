using System.Threading.Tasks;

namespace Ipam.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for managing repository lifecycles
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public interface IUnitOfWork
    {
        IAddressSpaceRepository AddressSpaces { get; }
        IIpAllocationRepository IpNodes { get; }
        ITagRepository Tags { get; }
        
        Task SaveChangesAsync();
    }
}
