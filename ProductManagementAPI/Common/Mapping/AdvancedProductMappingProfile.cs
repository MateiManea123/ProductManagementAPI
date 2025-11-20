using System;
using System.Globalization;
using AutoMapper;
using Features.Products;
using Features.Products.DTOs;

namespace Common.Mapping;

public class AdvancedProductMappingProfile : Profile
{
    public AdvancedProductMappingProfile()
    {
        CreateMap<CreateProductProfileRequest, Product>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(d => d.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.Price, opt => opt.MapFrom(src =>
                src.Category == ProductCategory.Home ? src.Price * 0.9m : src.Price));

        CreateMap<Product, ProductProfileDto>()
            .ForMember(d => d.CategoryDisplayName, opt => opt.MapFrom(src => 
                GetCategoryDisplayName(src.Category)))
            .ForMember(d => d.FormattedPrice, opt => opt.MapFrom(src =>
                src.Price.ToString("C2", CultureInfo.CurrentCulture)))
            .ForMember(d => d.ProductAge, opt => opt.MapFrom(src => 
                GetProductAge(src.ReleaseDate)))
            .ForMember(d => d.BrandInitials, opt => opt.MapFrom(src =>
                GetBrandInitials(src.Brand)))
            .ForMember(d => d.AvailabilityStatus, opt => opt.MapFrom(src =>
                GetAvailabilityStatus(src.IsAvailable, src.StockQuantity)))
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(src =>
                src.Category == ProductCategory.Home ? null : src.ImageUrl));
    }

    private static string GetCategoryDisplayName(ProductCategory category)
    {
        return category switch
        {
            ProductCategory.Electronics => "Electronics & Technology",
            ProductCategory.Clothing => "Clothing & Fashion",
            ProductCategory.Books => "Books & Media",
            ProductCategory.Home => "Home & Garden",
            _ => "Uncategorized"
        };
    }

    private static string GetProductAge(DateTime releaseDate)
    {
        var now = DateTime.UtcNow;
        var age = now - releaseDate;

        if (age.TotalDays < 30)
            return "New Release";

        if (age.TotalDays < 365)
        {
            var months = (int)(age.TotalDays / 30);
            if (months <= 0) months = 1;
            return $"{months} months old";
        }

        if (age.TotalDays < 1825)
        {
            var years = (int)(age.TotalDays / 365);
            if (years <= 0) years = 1;
            return $"{years} years old";
        }

        if (Math.Abs(age.TotalDays - 1825) < 1)
            return "Classic";

        var y = (int)(age.TotalDays / 365);
        return $"{y} years old";
    }

    private static string GetBrandInitials(string brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return "?";

        var parts = brand
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpperInvariant();

        var first = parts[0][0];
        var last = parts[^1][0];

        return $"{char.ToUpperInvariant(first)}{char.ToUpperInvariant(last)}";
    }

    private static string GetAvailabilityStatus(bool isAvailable, int stockQuantity)
    {
        if (!isAvailable)
            return "Out of Stock";

        if (stockQuantity <= 0)
            return "Unavailable";

        if (stockQuantity == 1)
            return "Last Item";

        if (stockQuantity <= 5)
            return "Limited Stock";

        return "In Stock";
    }
}
