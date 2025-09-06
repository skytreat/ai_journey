using System;

namespace Ipam.DataAccess.Exceptions
{
    /// <summary>
    /// Base exception class for IPAM data access layer
    /// </summary>
    public class IpamDataException : Exception
    {
        public IpamDataException(string message) : base(message) { }
        public IpamDataException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ConcurrencyException : IpamDataException
    {
        public ConcurrencyException(string message) : base(message) { }
    }

    public class EntityNotFoundException : IpamDataException
    {
        public EntityNotFoundException(string message) : base(message) { }
    }
}
