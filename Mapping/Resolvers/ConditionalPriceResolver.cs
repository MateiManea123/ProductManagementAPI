using System;
using AutoMapper;

namespace ProductModule
{
    public sealed class ConditionalPriceResolver : IValueResolver<Product, ProductProfileDto, decimal>
    {
        public decimal Resolve(Product source, ProductProfileDto destination, decimal destMember, ResolutionContext context)
        {
            // Apply 10% discount for Home category only
            return source.Category == ProductCategory.Home
                ? Math.Round(source.Price * 0.9m, 2, MidpointRounding.AwayFromZero)
                : source.Price;
        }
    }
}

