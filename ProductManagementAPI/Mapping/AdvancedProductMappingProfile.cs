using System;
using AutoMapper;

namespace ProductModule
{
    public sealed class AdvancedProductMappingProfile : Profile
    {
        public AdvancedProductMappingProfile()
        {
            CreateMap<CreateProductProfileRequest, Product>()
                .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.IsAvailable, opt => opt.MapFrom(s => s.StockQuantity > 0))
                .ForMember(d => d.UpdatedAt, opt => opt.Ignore());

            CreateMap<Product, ProductProfileDto>()
                .ForMember(d => d.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
                .ForMember(d => d.Price, opt => opt.MapFrom<ConditionalPriceResolver>())
                .ForMember(d => d.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
                .ForMember(d => d.ImageUrl, opt => opt.MapFrom<ConditionalImageUrlResolver>())
                .ForMember(d => d.ProductAge, opt => opt.MapFrom<ProductAgeResolver>())
                .ForMember(d => d.BrandInitials, opt => opt.MapFrom<BrandInitialsResolver>())
                .ForMember(d => d.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>());
        }
    }
}

