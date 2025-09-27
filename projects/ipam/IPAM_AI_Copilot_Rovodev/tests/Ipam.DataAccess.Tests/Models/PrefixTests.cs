using Xunit;
using Ipam.ServiceContract.Models;
using System;
using System.Net;

namespace Ipam.DataAccess.Tests.Models
{
    /// <summary>
    /// Unit tests for Prefix model
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class PrefixTests
    {
        [Theory]
        [InlineData("192.168.1.0/24", true)]
        [InlineData("10.0.0.0/8", true)]
        [InlineData("172.16.0.0/12", true)]
        [InlineData("2001:db8::/32", false)]
        [InlineData("fe80::/64", false)]
        public void Constructor_ValidCidr_CreatesPrefix(string cidr, bool expectedIsIPv4)
        {
            // Act
            var prefix = new Prefix(cidr);

            // Assert
            Assert.Equal(expectedIsIPv4, prefix.IsIPv4);
            Assert.Equal(cidr, prefix.ToString());
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("192.168.1.0")]
        [InlineData("192.168.1.0/")]
        [InlineData("192.168.1.0/33")]
        [InlineData("256.1.1.1/24")]
        [InlineData("192.168.1.0/-1")]
        public void Constructor_InvalidCidr_ThrowsArgumentException(string invalidCidr)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Prefix(invalidCidr));
        }

        [Fact]
        public void PrefixLength_IPv4Network_ReturnsCorrectLength()
        {
            // Arrange
            var prefix = new Prefix("192.168.1.0/24");

            // Act & Assert
            Assert.Equal(24, prefix.PrefixLength);
        }

        [Fact]
        public void PrefixLength_IPv6Network_ReturnsCorrectLength()
        {
            // Arrange
            var prefix = new Prefix("2001:db8::/64");

            // Act & Assert
            Assert.Equal(64, prefix.PrefixLength);
        }

        [Theory]
        [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
        [InlineData("10.0.0.0/8", "192.168.1.0/24", false)]
        [InlineData("192.168.0.0/16", "192.168.1.0/24", true)]
        [InlineData("192.168.1.0/24", "192.168.0.0/16", false)]
        public void Contains_VariousNetworks_ReturnsCorrectResult(string parentCidr, string childCidr, bool expectedResult)
        {
            // Arrange
            var parent = new Prefix(parentCidr);
            var child = new Prefix(childCidr);

            // Act
            var result = parent.Contains(child);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("192.168.1.0/24", "192.168.0.0/16", true)]
        [InlineData("10.1.0.0/16", "10.0.0.0/8", true)]
        [InlineData("192.168.1.0/24", "10.0.0.0/8", false)]
        [InlineData("192.168.0.0/16", "192.168.1.0/24", false)]
        public void IsSubnetOf_VariousNetworks_ReturnsCorrectResult(string subnetCidr, string parentCidr, bool expectedResult)
        {
            // Arrange
            var subnet = new Prefix(subnetCidr);
            var parent = new Prefix(parentCidr);

            // Act
            var result = subnet.IsSubnetOf(parent);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("192.168.0.0/16", "192.168.1.0/24", true)]
        [InlineData("192.168.1.0/24", "192.168.1.128/25", true)]
        [InlineData("192.168.1.0/24", "192.168.1.0/25", true)]
        [InlineData("192.168.1.0/25", "192.168.1.0/24", false)]
        public void IsSupernetOf_VariousNetworks_ReturnsCorrectResult(string supernetCidr, string subnetCidr, bool expectedResult)
        {
            // Arrange
            var supernet = new Prefix(supernetCidr);
            var subnet = new Prefix(subnetCidr);

            // Act
            var result = supernet.IsSupernetOf(subnet);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void GetSubnets_IPv4Network_ReturnsCorrectSubnets()
        {
            // Arrange
            var prefix = new Prefix("192.168.1.0/24");

            // Act
            var subnets = prefix.GetSubnets();

            // Assert
            Assert.Equal(2, subnets.Count);
            Assert.Contains(subnets, s => s.ToString() == "192.168.1.0/25");
            Assert.Contains(subnets, s => s.ToString() == "192.168.1.128/25");
        }

        [Fact]
        public void GetSubnets_IPv6Network_ReturnsCorrectSubnets()
        {
            // Arrange
            var prefix = new Prefix("2001:db8::/64");

            // Act
            var subnets = prefix.GetSubnets();

            // Assert
            Assert.Equal(2, subnets.Count);
            Assert.All(subnets, s => Assert.Equal(65, s.PrefixLength));
        }

        [Theory]
        [InlineData("192.168.1.0/30")]
        [InlineData("2001:db8::/127")]
        public void GetSubnets_NearMaxPrefixLength_ReturnsSubnets(string cidr)
        {
            // Arrange
            var prefix = new Prefix(cidr);

            // Act
            var subnets = prefix.GetSubnets();

            // Assert
            Assert.Equal(2, subnets.Count);
            Assert.All(subnets, s => Assert.Equal(prefix.PrefixLength + 1, s.PrefixLength));
        }

        [Theory]
        [InlineData("192.168.1.0/32")]
        [InlineData("2001:db8::1/128")]
        public void GetSubnets_MaxPrefixLength_ReturnsEmptyList(string cidr)
        {
            // Arrange
            var prefix = new Prefix(cidr);

            // Act
            var subnets = prefix.GetSubnets();

            // Assert
            Assert.Empty(subnets);
        }

        [Theory]
        [InlineData("192.168.1.0/24", "192.168.1.0/24", true)]
        [InlineData("192.168.1.0/24", "192.168.1.0/25", false)]
        [InlineData("2001:db8::/64", "2001:db8::/64", true)]
        public void Equals_VariousPrefixes_ReturnsCorrectResult(string cidr1, string cidr2, bool expectedResult)
        {
            // Arrange
            var prefix1 = new Prefix(cidr1);
            var prefix2 = new Prefix(cidr2);

            // Act
            var result = prefix1.Equals(prefix2);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void CompareTo_SortsPrefixesCorrectly()
        {
            // Arrange
            var prefixes = new[]
            {
                new Prefix("192.168.2.0/24"),
                new Prefix("192.168.1.0/24"),
                new Prefix("10.0.0.0/8"),
                new Prefix("192.168.1.0/25")
            };

            // Act
            Array.Sort(prefixes);

            // Assert
            Assert.Equal("10.0.0.0/8", prefixes[0].ToString());
            Assert.Equal("192.168.1.0/24", prefixes[1].ToString());
            Assert.Equal("192.168.1.0/25", prefixes[2].ToString());
            Assert.Equal("192.168.2.0/24", prefixes[3].ToString());
        }

        [Theory]
        [InlineData("192.168.1.0/24", "192.168.1.0/24", 0)]
        [InlineData("10.0.0.0/8", "192.168.1.0/24", -1)]
        [InlineData("192.168.1.0/24", "10.0.0.0/8", 1)]
        public void CompareTo_VariousPrefixes_ReturnsCorrectComparison(string cidr1, string cidr2, int expectedSign)
        {
            // Arrange
            var prefix1 = new Prefix(cidr1);
            var prefix2 = new Prefix(cidr2);

            // Act
            var result = prefix1.CompareTo(prefix2);

            // Assert
            if (expectedSign == 0)
                Assert.Equal(0, result);
            else if (expectedSign < 0)
                Assert.True(result < 0);
            else
                Assert.True(result > 0);
        }

        [Fact]
        public void GetHashCode_EqualPrefixes_ReturnsSameHashCode()
        {
            // Arrange
            var prefix1 = new Prefix("192.168.1.0/24");
            var prefix2 = new Prefix("192.168.1.0/24");

            // Act
            var hash1 = prefix1.GetHashCode();
            var hash2 = prefix2.GetHashCode();

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void GetHashCode_DifferentPrefixes_ReturnsDifferentHashCodes()
        {
            // Arrange
            var prefix1 = new Prefix("192.168.1.0/24");
            var prefix2 = new Prefix("192.168.2.0/24");

            // Act
            var hash1 = prefix1.GetHashCode();
            var hash2 = prefix2.GetHashCode();

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData("192.168.1.0/24", "192.168.1.0", 24)]
        [InlineData("10.0.0.0/8", "10.0.0.0", 8)]
        [InlineData("2001:db8::/32", "2001:db8::", 32)]
        public void Address_VariousPrefixes_ReturnsCorrectAddress(string cidr, string expectedAddress, int expectedPrefixLength)
        {
            // Arrange
            var prefix = new Prefix(cidr);

            // Act
            var address = prefix.Address;
            var prefixLength = prefix.PrefixLength;

            // Assert
            Assert.Equal(IPAddress.Parse(expectedAddress), address);
            Assert.Equal(expectedPrefixLength, prefixLength);
        }
    }
}