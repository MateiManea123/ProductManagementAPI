using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    private static readonly Regex SkuRegex = new(@"^[A-Za-z0-9\-]{5,20}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return new ValidationResult("SKU is required.");

        var sku = value.ToString()!.Replace(" ", string.Empty);

        if (!SkuRegex.IsMatch(sku))
            return new ValidationResult("SKU must be alphanumeric with hyphens and 5-20 characters.");

        return ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-validsku", ErrorMessage ??
                                                                "SKU must be alphanumeric with hyphens and 5-20 characters.");
        MergeAttribute(context.Attributes, "data-val-validsku-pattern", SkuRegex.ToString());
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
            attributes.Add(key, value);
    }
}