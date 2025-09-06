using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ipam.Frontend.Validation
{
    /// <summary>
    /// Validation attribute for IP node tags
    /// </summary>
    public class TagsValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            var tags = value as Dictionary<string, string>;
            if (tags == null)
                return new ValidationResult("Invalid tags format");

            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.Key))
                    return new ValidationResult("Tag key cannot be empty");
                
                if (string.IsNullOrWhiteSpace(tag.Value))
                    return new ValidationResult("Tag value cannot be empty");
            }

            return ValidationResult.Success;
        }
    }
}
