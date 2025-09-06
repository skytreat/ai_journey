using System;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Ipam.Frontend.Validation
{
    /// <summary>
    /// Validation attribute for CIDR format
    /// </summary>
    /// <remarks>
    /// Author: IPAM Team
    /// Date: 2024-01-20
    /// </remarks>
    public class CidrValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            var cidr = value.ToString();
            var parts = cidr.Split('/');

            if (parts.Length != 2)
                return new ValidationResult("Invalid CIDR format");

            if (!IPAddress.TryParse(parts[0], out _))
                return new ValidationResult("Invalid IP address");

            // Support both IPv4 and IPv6
            var isIPv6 = parts[0].Contains(':');
            var maxPrefix = isIPv6 ? 128 : 32;
            
            if (!int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > maxPrefix)
                return new ValidationResult($"Invalid prefix length. Must be 0-{maxPrefix} for {(isIPv6 ? "IPv6" : "IPv4")}");

            return ValidationResult.Success;
        }
    }
}
