using FluentAssertions;
using IPAM.Infrastructure;

namespace Domain.Tests;

public class BasicCidrServiceTests
{
    private readonly BasicCidrService _cidrService;

    public BasicCidrServiceTests()
    {
        _cidrService = new BasicCidrService();
    }

    [Theory]
    [InlineData("192.168.1.0/24", true)]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("172.16.0.0/16", true)]
    [InlineData("192.168.1.1/32", true)]
    [InlineData("0.0.0.0/0", true)]
    [InlineData("255.255.255.255/32", true)]
    public void IsValidCidr_ShouldReturnTrueForValidIPv4Cidrs(string cidr, bool expected)
    {
        // Act
        var result = _cidrService.IsValidCidr(cidr);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2001:db8::/32", true)]
    [InlineData("::1/128", true)]
    [InlineData("2001:db8:85a3::8a2e:370:7334/128", true)]
    [InlineData("::/0", true)]
    [InlineData("fe80::/10", true)]
    [InlineData("ff00::/8", true)]
    public void IsValidCidr_ShouldReturnTrueForValidIPv6Cidrs(string cidr, bool expected)
    {
        // Act
        var result = _cidrService.IsValidCidr(cidr);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("192.168.1.0", false)] // Missing subnet mask
    [InlineData("192.168.1.0/", false)] // Empty subnet mask
    [InlineData("192.168.1.0/33", false)] // Invalid subnet mask for IPv4
    [InlineData("192.168.1.0/-1", false)] // Negative subnet mask
    [InlineData("256.256.256.256/24", false)] // Invalid IP address
    [InlineData("192.168.1/24", false)] // Incomplete IP address
    [InlineData("", false)] // Empty string
    [InlineData(null, false)] // Null string
    [InlineData("not-an-ip/24", false)] // Invalid format
    [InlineData("192.168.1.0/abc", false)] // Non-numeric subnet mask
    public void IsValidCidr_ShouldReturnFalseForInvalidCidrs(string cidr, bool expected)
    {
        // Act
        var result = _cidrService.IsValidCidr(cidr);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("2001:db8::/129", false)] // Invalid subnet mask for IPv6
    [InlineData("2001:db8::/-1", false)] // Negative subnet mask
    [InlineData("gggg::/32", false)] // Invalid IPv6 address
    [InlineData("2001:db8::/abc", false)] // Non-numeric subnet mask
    public void IsValidCidr_ShouldReturnFalseForInvalidIPv6Cidrs(string cidr, bool expected)
    {
        // Act
        var result = _cidrService.IsValidCidr(cidr);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidCidr_ShouldHandleWhitespace()
    {
        // Arrange
        var cidrWithSpaces = " 192.168.1.0/24 ";

        // Act
        var result = _cidrService.IsValidCidr(cidrWithSpaces);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("192.168.1.0/24")]
    [InlineData("10.0.0.0/8")]
    [InlineData("2001:db8::/32")]
    [InlineData("::1/128")]
    public void IsValidCidr_ShouldBeConsistentForMultipleCalls(string cidr)
    {
        // Act
        var result1 = _cidrService.IsValidCidr(cidr);
        var result2 = _cidrService.IsValidCidr(cidr);
        var result3 = _cidrService.IsValidCidr(cidr);

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
        result1.Should().BeTrue(); // These are all valid CIDRs
    }

    [Fact]
    public void IsValidCidr_ShouldHandleConcurrentCalls()
    {
        // Arrange
        var cidrs = new[]
        {
            "192.168.1.0/24",
            "10.0.0.0/8",
            "172.16.0.0/16",
            "2001:db8::/32",
            "::1/128"
        };

        // Act & Assert
        Parallel.ForEach(cidrs, cidr =>
        {
            var result = _cidrService.IsValidCidr(cidr);
            result.Should().BeTrue();
        });
    }
}
