using System.Linq;
using AutoMapper;

namespace ProductModule
{
    public sealed class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
    {
        public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source.Brand)) return "?";
            var parts = source.Brand
                .Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpperInvariant();
            var first = parts.First()[0];
            var last = parts.Last()[0];
            return char.ToUpperInvariant(first) + char.ToUpperInvariant(last).ToString();
        }
    }
}

