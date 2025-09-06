using Xunit;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ipam.Frontend.Validation;

namespace Ipam.Frontend.Tests.Validation
{
    public class TagsValidationAttributeTests
    {
        private readonly TagsValidationAttribute _validator;

        public TagsValidationAttributeTests()
        {
            _validator = new TagsValidationAttribute();
        }

        [Fact]
        public void IsValid_NullTags_ReturnsSuccess()
        {
            var result = _validator.IsValid(null, new ValidationContext(new object()));
            Assert.Equal(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_ValidTags_ReturnsSuccess()
        {
            var tags = new Dictionary<string, string>
            {
                { "Environment", "Production" },
                { "Region", "USEast" }
            };

            var result = _validator.IsValid(tags, new ValidationContext(new object()));
            Assert.Equal(ValidationResult.Success, result);
        }

        [Fact]
        public void IsValid_EmptyKey_ReturnsError()
        {
            var tags = new Dictionary<string, string>
            {
                { "", "Production" }
            };

            var result = _validator.IsValid(tags, new ValidationContext(new object()));
            Assert.NotEqual(ValidationResult.Success, result);
        }
    }
}
