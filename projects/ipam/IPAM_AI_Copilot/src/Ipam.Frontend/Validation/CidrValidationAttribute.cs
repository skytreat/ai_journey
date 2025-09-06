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

            if (!int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > 32)
                return new ValidationResult("Invalid prefix length");

            return ValidationResult.Success;
        }
    }
}
