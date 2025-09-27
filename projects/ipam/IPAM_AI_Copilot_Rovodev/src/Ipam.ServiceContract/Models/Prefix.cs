using System.Numerics;

namespace Ipam.ServiceContract.Models
{
    /// <summary>
    /// Represents an IP prefix in CIDR notation
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class Prefix : IEquatable<Prefix>, IComparable<Prefix>
    {
        public System.Net.IPAddress Address { get; }
        public int PrefixLength { get; }
        public bool IsIPv4 { get; }
        private readonly BigInteger _numericValue;
        private readonly BigInteger _mask;
        private const int _maxPrefixLength = 128; // Maximum prefix length for IPv6

        public Prefix(string cidr)
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid CIDR format", nameof(cidr));

            try
            {
                Address = System.Net.IPAddress.Parse(parts[0]);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid IP address format", nameof(cidr), ex);
            }

            try
            {
                PrefixLength = int.Parse(parts[1]);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid prefix length format", nameof(cidr), ex);
            }
            IsIPv4 = Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;

            if (IsIPv4 && (PrefixLength < 0 || PrefixLength > 32))
                throw new ArgumentException("Invalid IPv4 prefix length");
            if (!IsIPv4 && (PrefixLength < 0 || PrefixLength > 128))
                throw new ArgumentException("Invalid IPv6 prefix length");

            _numericValue = ToBigInteger(Address);
            _mask = CreateMask(PrefixLength, IsIPv4);
        }

        public override string ToString() => $"{Address}/{PrefixLength}";

        public bool Contains(Prefix other)
        {
            if (IsIPv4 != other.IsIPv4) return false;
            if (PrefixLength > other.PrefixLength) return false;
            
            var thisNetwork = _numericValue & _mask;
            var otherNetwork = other._numericValue & _mask;
            return thisNetwork == otherNetwork;
        }

        public bool IsSupernetOf(Prefix other) => 
            PrefixLength < other.PrefixLength && Contains(other);

        public bool IsSubnetOf(Prefix other) => 
            other.PrefixLength < PrefixLength && other.Contains(this);

        public List<Prefix> GetSubnets(int newPrefixLength = -1)
        {
            if(newPrefixLength == -1)
                newPrefixLength = PrefixLength + 1;
                
            if (IsIPv4 && newPrefixLength > 32)
                throw new ArgumentException($"New prefix length must be less than or equal to 32 for IPv4");
            if (!IsIPv4 && newPrefixLength > 128)
                throw new ArgumentException($"New prefix length must be less than or equal to 128 for IPv6");
            
            if (newPrefixLength <= PrefixLength || newPrefixLength > _maxPrefixLength)
                throw new ArgumentException($"New prefix length must be greater than current ({PrefixLength}) and less than or equal to {_maxPrefixLength}");

            // Calculate the network address for the current prefix
            var networkAddress = _numericValue & _mask;
            var subnetCount = 1 << (newPrefixLength - PrefixLength);
            var subnets = new List<Prefix>(subnetCount);
            
            var increment = BigInteger.One << (_maxPrefixLength - newPrefixLength);
            for (int i = 0; i < subnetCount; i++)
            {
                subnets.Add(new Prefix($"{ToIPAddress(networkAddress + (increment * i))}/{newPrefixLength}"));
            }

            return subnets;
        }

        private static BigInteger ToBigInteger(System.Net.IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());
        }

        private System.Net.IPAddress ToIPAddress(BigInteger value)
        {
            var bytes = value.ToByteArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return new System.Net.IPAddress(bytes.Take(IsIPv4 ? 4 : 16).ToArray());
        }

        private static BigInteger CreateMask(int length, bool isIPv4)
        {
            int totalBits = isIPv4 ? 32 : 128;
            if (length == 0) return BigInteger.Zero;
            return (BigInteger.One << totalBits) - (BigInteger.One << (totalBits - length));
        }

        public bool Equals(Prefix? other)
        {
            if (other is null) return false;
            return Address.Equals(other.Address) && PrefixLength == other.PrefixLength;
        }

        public override bool Equals(object? obj) => 
            obj is Prefix prefix && Equals(prefix);

        public int CompareTo(Prefix? other)
        {
            if (other is null) return 1;
            int addressComparison = string.Compare(Address.ToString(), other.Address.ToString(), StringComparison.Ordinal);
            if (addressComparison != 0) return addressComparison;
            return PrefixLength.CompareTo(other.PrefixLength);
        }

        public override int GetHashCode() => HashCode.Combine(Address, PrefixLength);
    }
}