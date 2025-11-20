using System;
using Features.Products;
using MediatR;

namespace Features.Products.DTOs;

public class ProductProfileDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string SKU { get; set; } = default!;

    public string CategoryDisplayName { get; set; } = default!;

    public decimal Price { get; set; }
    public string FormattedPrice { get; set; } = default!;

    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }

    public string ProductAge { get; set; } = default!;
    public string BrandInitials { get; set; } = default!;
    public string AvailabilityStatus { get; set; } = default!;
}

public class CreateProductProfileRequest : IRequest<ProductProfileDto>
{
    public string Name { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; } = 1;
}