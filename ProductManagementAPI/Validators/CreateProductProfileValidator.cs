using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Features.Products;
using Features.Products.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ApplicationContext _context;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    private static readonly string[] InappropriateWords = { "badword1", "badword2" };
    private static readonly string[] TechnologyKeywords =
    {
        "smart", "pro", "ultra", "4k", "hd", "wifi", "bluetooth", "gaming", "pc", "laptop", "tablet"
    };
    private static readonly string[] HomeRestrictedWords = { "weapon", "explosive" };

    public CreateProductProfileValidator(
        ApplicationContext context,
        ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(1)
            .MaximumLength(200)
            .Must(BeValidName).WithMessage("Product name contains inappropriate content.")
            .MustAsync(BeUniqueName).WithMessage("A product with the same name and brand already exists.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MinimumLength(2)
            .MaximumLength(100)
            .Must(BeValidBrandName).WithMessage("Brand contains invalid characters.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .Must(BeValidSKU).WithMessage("SKU must be alphanumeric with hyphens and 5-20 characters.")
            .MustAsync(BeUniqueSKU).WithMessage("SKU already exists.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category must be a valid value.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThan(10000);

        RuleFor(x => x.ReleaseDate)
            .Must(d => d <= DateTime.UtcNow).WithMessage("Release date cannot be in the future.")
            .Must(d => d.Year >= 1900).WithMessage("Release date cannot be before year 1900.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100000);

        When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl!)
                .Must(BeValidImageUrl).WithMessage("Image URL must be a valid HTTP/HTTPS image URL.");
        });

        RuleFor(x => x)
            .MustAsync(PassBusinessRules).WithMessage("Business rules validation failed.");
        
        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(50m).WithMessage("Electronics must have a minimum price of $50.00.");

            RuleFor(x => x.Name)
                .Must(ContainTechnologyKeywords)
                .WithMessage("Electronics products must contain technology-related keywords in the name.");

            RuleFor(x => x.ReleaseDate)
                .Must(BeRecentElectronics)
                .WithMessage("Electronics products must be released within the last 5 years.");
        });

        When(x => x.Category == ProductCategory.Home, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(200m).WithMessage("Home products must not exceed $200.00.");

            RuleFor(x => x.Name)
                .Must(BeAppropriateForHome)
                .WithMessage("Home product name contains inappropriate words.");
        });

        When(x => x.Category == ProductCategory.Clothing, () =>
        {
            RuleFor(x => x.Brand)
                .MinimumLength(3).WithMessage("Clothing brand must be at least 3 characters.");
        });

        RuleFor(x => x)
            .Must(p => p.Price <= 100m || p.StockQuantity <= 20)
            .WithMessage("Expensive products (>$100) must have limited stock (≤20 units).");
    }

    private bool BeValidName(string name)
    {
        var lower = name.ToLowerInvariant();
        return !InappropriateWords.Any(w => lower.Contains(w));
    }

    private async Task<bool> BeUniqueName(CreateProductProfileRequest request, string name, CancellationToken ct)
    {
        return !await _context.Products
            .AnyAsync(p => p.Name == request.Name && p.Brand == request.Brand, ct);
    }

    private bool BeValidBrandName(string brand)
    {
        var regex = new Regex(@"^[\p{L}\p{N}\s\-\.'']+$");
        return regex.IsMatch(brand);
    }

    private bool BeValidSKU(string sku)
    {
        sku = sku.Replace(" ", string.Empty);
        return Regex.IsMatch(sku, @"^[A-Za-z0-9\-]{5,20}$");
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken ct)
    {
        return !await _context.Products.AnyAsync(p => p.SKU == sku, ct);
    }

    private bool BeValidImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var lower = uri.AbsolutePath.ToLowerInvariant();
        return lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") ||
               lower.EndsWith(".png") || lower.EndsWith(".gif") ||
               lower.EndsWith(".webp");
    }

    private async Task<bool> PassBusinessRules(CreateProductProfileRequest request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var countToday = await _context.Products
            .CountAsync(p => p.CreatedAt >= today && p.CreatedAt < today.AddDays(1), ct);

        if (countToday >= 500)
            return false;

        if (request.Category == ProductCategory.Electronics && request.Price < 50m)
            return false;

        if (request.Category == ProductCategory.Home && !BeAppropriateForHome(request.Name))
            return false;

        if (request.Price > 500m && request.StockQuantity > 10)
            return false;

        return true;
    }

    private bool ContainTechnologyKeywords(string name)
    {
        var lower = name.ToLowerInvariant();
        return TechnologyKeywords.Any(k => lower.Contains(k));
    }

    private bool BeAppropriateForHome(string name)
    {
        var lower = name.ToLowerInvariant();
        return !HomeRestrictedWords.Any(w => lower.Contains(w));
    }

    private bool BeRecentElectronics(DateTime releaseDate)
    {
        return releaseDate >= DateTime.UtcNow.AddYears(-5);
    }
}
