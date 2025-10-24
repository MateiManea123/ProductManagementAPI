using AutoMapper;

namespace ProductModule
{
    public sealed class ConditionalImageUrlResolver : IValueResolver<Product, ProductProfileDto, string?>
    {
        public string? Resolve(Product source, ProductProfileDto destination, string? destMember, ResolutionContext context)
        {
            if (source.Category == ProductCategory.Home)
                return null;
            return source.ImageUrl;
        }
    }
}

