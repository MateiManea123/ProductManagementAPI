using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Validators.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ProductCategoryAttribute : ValidationAttribute
    {
        private readonly int[] _allowedCategories;

        public ProductCategoryAttribute(params int[] allowedCategories)
        {
            _allowedCategories = allowedCategories;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult(GetErrorMessage());

            var intValue = (int)value;
            if (!_allowedCategories.Contains(intValue))
                return new ValidationResult(GetErrorMessage());

            return ValidationResult.Success;
        }

        private string GetErrorMessage()
        {
            var allowedList = string.Join(", ", _allowedCategories);
            return ErrorMessage ?? $"Category must be one of the following: {allowedList}.";
        }
    }
}