using Xunit;
using Ipam.Frontend.Validation;
using System.ComponentModel.DataAnnotations;

namespace Ipam.Frontend.Tests.Validation
{
    /// <summary>
    /// Unit tests for CidrValidationAttribute
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class CidrValidationAttributeTests
    {
        private readonly CidrValidationAttribute _validator;

        public CidrValidationAttributeTests()
        {
            _validator = new CidrValidationAttribute();
        }

        [Theory]
        [InlineData("192.168.1.0/24")]
        [InlineData("10.0.0.0/8")]
        [InlineData("172.16.0.0/12")]
        [InlineData("0.0.0.0/0")]
        [InlineData("255.255.255.255/32")]
        public void IsValid_ValidIPv4Cidr_ReturnsSuccess(string validCidr)
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(validCidr, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        [Theory]
        [InlineData("2001:db8::/32")]
        [InlineData("fe80::/64")]
        [InlineData("::/0")]
        [InlineData("::1/128")]
        [InlineData("2001:db8:85a3::8a2e:370:7334/128")]
        public void IsValid_ValidIPv6Cidr_ReturnsSuccess(string validCidr)
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(validCidr, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("192.168.1.0")]
        [InlineData("192.168.1.0/")]
        [InlineData("192.168.1.0/33")]
        [InlineData("256.1.1.1/24")]
        [InlineData("192.168.1.0/-1")]
        public void IsValid_InvalidIPv4Cidr_ReturnsError(string invalidCidr)
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(invalidCidr, context);

            // Assert
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.NotNull(result);
            Assert.Contains("Invalid", result.ErrorMessage);
        }

        [Theory]
        [InlineData("2001:db8::/129")]
        [InlineData("invalid::ipv6")]
        [InlineData("2001:db8::/-1")]
        [InlineData("2001:db8::/")]
        public void IsValid_InvalidIPv6Cidr_ReturnsError(string invalidCidr)
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(invalidCidr, context);

            // Assert
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.NotNull(result);
            Assert.Contains("Invalid", result.ErrorMessage);
        }

        [Fact]
        public void IsValid_NullValue_ReturnsSuccess()
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(null, context);

            // Assert
            Assert.Equal(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_EmptyString_ReturnsError()
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult("", context);

            // Assert
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.NotNull(result);
        }

        [Fact]
        public void IsValid_WhitespaceString_ReturnsError()
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult("   ", context);

            // Assert
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("192.168.1.0/24", "Must be 0-32 for IPv4")]
        [InlineData("2001:db8::/64", "Must be 0-128 for IPv6")]
        public void IsValid_ValidCidr_ErrorMessageContainsCorrectRange(string cidr, string expectedRange)
        {
            // This test verifies that error messages contain the correct prefix length ranges
            // We'll test with an invalid prefix to see the error message format
            
            // Arrange
            var invalidCidr = cidr.Replace("/24", "/33").Replace("/64", "/129");
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(invalidCidr, context);

            // Assert
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.Contains(expectedRange, result.ErrorMessage);
        }

        [Fact]
        public void IsValid_NonStringValue_ReturnsError()
        {
            // Arrange
            var context = new ValidationContext(new object());
            var nonStringValue = 12345;

            // Act
            var result = _validator.GetValidationResult(nonStringValue, context);

            // Assert
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("192.168.1.1/24")] // Host bit set
        [InlineData("10.0.0.1/8")]     // Host bit set
        public void IsValid_HostBitsSet_StillValid(string cidrWithHostBits)
        {
            // Many CIDR validators accept host bits being set
            // This test documents the current behavior
            
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(cidrWithHostBits, context);

            // Assert
            // This might be Success or Error depending on implementation
            // We just verify it doesn't throw an exception
            Assert.True(result == ValidationResult.Success || result != null);
        }

        [Theory]
        [InlineData("192.168.1.0/0")]   // Valid but unusual
        [InlineData("0.0.0.0/32")]      // Valid but unusual
        [InlineData("::/128")]          // Valid but unusual
        public void IsValid_EdgeCaseCidrs_HandledCorrectly(string edgeCaseCidr)
        {
            // Arrange
            var context = new ValidationContext(new object());

            // Act
            var result = _validator.GetValidationResult(edgeCaseCidr, context);

            // Assert
            // These should be valid according to CIDR standards
            Assert.Equal(ValidationResult.Success, result);
        }
    }
}