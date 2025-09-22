using Xunit;
using Ipam.DataAccess.Validation;
using System;
using System.Collections.Generic;
using Ipam.ServiceContract.Models;

namespace Ipam.DataAccess.Tests.Validation
{
    /// <summary>
    /// Unit tests for IpamValidator
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class IpamValidatorTests
    {
        [Theory]
        [InlineData("192.168.1.0/24")]
        [InlineData("10.0.0.0/8")]
        [InlineData("172.16.0.0/12")]
        [InlineData("0.0.0.0/0")]
        [InlineData("255.255.255.255/32")]
        [InlineData("2001:db8::/32")]
        [InlineData("fe80::/64")]
        [InlineData("::/0")]
        [InlineData("::1/128")]
        public void ValidateCidr_ValidCidr_DoesNotThrow(string validCidr)
        {
            // Act & Assert
            IpamValidator.ValidateCidr(validCidr);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("192.168.1.0")]
        [InlineData("192.168.1.0/")]
        [InlineData("192.168.1.0/33")]
        [InlineData("256.1.1.1/24")]
        [InlineData("192.168.1.0/-1")]
        [InlineData("192.168.1.0/129")]
        [InlineData("2001:db8::/129")]
        [InlineData("")]
        [InlineData(null)]
        public void ValidateCidr_InvalidCidr_ThrowsArgumentException(string invalidCidr)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => IpamValidator.ValidateCidr(invalidCidr));
        }

        [Fact]
        public void ValidateTagInheritance_NoConflicts_DoesNotThrow()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "USEast" }
            };

            var childTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Application", "WebServer" }
            };

            // Act & Assert
            IpamValidator.ValidateTagInheritance(parentTags, childTags);
        }

        [Fact]
        public void ValidateTagInheritance_WithConflicts_ThrowsArgumentException()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" }
            };

            var childTags = new Dictionary<string, string>
            {
                { "Environment", "Development" }
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => IpamValidator.ValidateTagInheritance(parentTags, childTags));
        }

        [Fact]
        public void ValidateTagInheritance_NullOrEmptyTags_DoesNotThrow()
        {
            IpamValidator.ValidateTagInheritance(null, new Dictionary<string, string>());
            IpamValidator.ValidateTagInheritance(new Dictionary<string, string>(), null);
            IpamValidator.ValidateTagInheritance(null, null);
        }

        [Fact]
        public void ValidateTagInheritance_EmptyParentTags_DoesNotThrow()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>();
            var childTags = new Dictionary<string, string>
            {
                { "Application", "WebServer" }
            };

            // Act & Assert
            IpamValidator.ValidateTagInheritance(parentTags, childTags);
        }

        [Fact]
        public void ValidateTagInheritance_EmptyChildTags_DoesNotThrow()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" }
            };
            var childTags = new Dictionary<string, string>();

            // Act & Assert
            IpamValidator.ValidateTagInheritance(parentTags, childTags);
        }

        [Fact]
        public void ValidateTagInheritance_PartialOverlap_AllowsNonConflictingTags()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "USEast" }
            };

            var childTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },  // Same value - OK
                { "Application", "WebServer" },   // New tag - OK
                { "Tier", "Frontend" }            // New tag - OK
            };

            // Act & Assert
            IpamValidator.ValidateTagInheritance(parentTags, childTags);
        }

        [Fact]
        public void ValidateTagInheritance_MultipleConflicts_ThrowsWithFirstConflict()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "USEast" }
            };

            var childTags = new Dictionary<string, string>
            {
                { "Environment", "Development" },  // Conflict
                { "Region", "USWest" }              // Another conflict
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => IpamValidator.ValidateTagInheritance(parentTags, childTags));
            Assert.Contains("Environment", exception.Message);
        }

        [Theory]
        [InlineData("Environment", "")]
        [InlineData("", "Production")]
        [InlineData("", "")]
        public void ValidateTagInheritance_EmptyKeysOrValues_HandlesGracefully(string key, string value)
        {
            // Arrange
            var parentTags = new Dictionary<string, string>();
            var childTags = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(key) || !string.IsNullOrEmpty(value))
            {
                childTags[key] = value;
            }

            // Act & Assert - Should not throw for empty keys/values
            IpamValidator.ValidateTagInheritance(parentTags, childTags);
        }

        [Fact]
        public void ValidateTagInheritance_CaseSensitiveComparison_TreatsAsConflict()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" }
            };

            var childTags = new Dictionary<string, string>
            {
                { "Environment", "production" }  // Different case
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => IpamValidator.ValidateTagInheritance(parentTags, childTags));
        }

        [Fact]
        public void ValidateTagInheritance_CaseSensitiveKeys_TreatsAsDifferentTags()
        {
            // Arrange
            var parentTags = new Dictionary<string, string>
            {
                { "Environment", "Production" }
            };

            var childTags = new Dictionary<string, string>
            {
                { "environment", "Development" }  // Different case key
            };

            // Act & Assert - Should not throw as keys are different
            IpamValidator.ValidateTagInheritance(parentTags, childTags);
        }

        [Theory]
        [InlineData("192.168.1.0/24", "192.168.1.0/25", true)]
        [InlineData("10.0.0.0/8", "10.1.0.0/16", true)]
        [InlineData("192.168.1.0/24", "192.168.2.0/24", false)]
        [InlineData("172.16.0.0/12", "192.168.1.0/24", false)]
        public void ValidateSubnetRelationship_VariousNetworks_ReturnsCorrectResult(
            string parentCidr, string childCidr, bool shouldBeValid)
        {
            // This test assumes there's a method for validating subnet relationships
            // If it doesn't exist, we can test the underlying logic through Prefix class
            
            // Arrange
            var parentPrefix = new Prefix(parentCidr);
            var childPrefix = new Prefix(childCidr);

            // Act
            var isSubnet = childPrefix.IsSubnetOf(parentPrefix);

            // Assert
            Assert.Equal(shouldBeValid, isSubnet);
        }
    }
}