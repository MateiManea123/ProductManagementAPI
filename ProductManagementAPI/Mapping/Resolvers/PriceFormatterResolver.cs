using System.Globalization;
using AutoMapper;

namespace ProductModule
{
    public sealed class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
    {
        public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
        {
            // Format destination.Price (which may already include discount) for currency
            var effectivePrice = destination.Price != default
                ? destination.Price
                : source.Category == ProductCategory.Home ? System.Math.Round(source.Price * 0.9m, 2, System.MidpointRounding.AwayFromZero) : source.Price;

            return effectivePrice.ToString("C2", CultureInfo.CurrentCulture);
        }
    }
}

