using AutoMapper;

namespace ProductModule
{
    public sealed class CategoryDisplayResolver : IValueResolver<Product, ProductProfileDto, string>
    {
        public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
        {
            return source.Category switch
            {
                ProductCategory.Electronics => "Electronics & Technology",
                ProductCategory.Clothing => "Clothing & Fashion",
                ProductCategory.Books => "Books & Media",
                ProductCategory.Home => "Home & Garden",
                _ => "Uncategorized"
            };
        }
    }
}

