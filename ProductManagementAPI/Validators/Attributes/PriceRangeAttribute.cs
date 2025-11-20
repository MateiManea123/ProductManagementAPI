using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _min;
    private readonly decimal _max;

    public PriceRangeAttribute(double min, double max)
    {
        _min = Convert.ToDecimal(min, CultureInfo.InvariantCulture);
        _max = Convert.ToDecimal(max, CultureInfo.InvariantCulture);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return new ValidationResult(GetErrorMessage());

        if (value is not decimal d)
            return new ValidationResult("Invalid price value.");

        if (d < _min || d > _max)
            return new ValidationResult(GetErrorMessage());

        return ValidationResult.Success;
    }

    private string GetErrorMessage()
    {
        var minStr = _min.ToString("C2", CultureInfo.CurrentCulture);
        var maxStr = _max.ToString("C2", CultureInfo.CurrentCulture);
        return ErrorMessage ?? $"Price must be between {minStr} and {maxStr}.";
    }
}