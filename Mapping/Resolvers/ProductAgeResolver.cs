using System;
using AutoMapper;

namespace ProductModule
{
    public sealed class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
    {
        public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
        {
            var now = DateTime.UtcNow.Date;
            var release = source.ReleaseDate.Date;
            var days = (now - release).TotalDays;
            if (days < 0)
                return "Releases in the future";

            if (days < 30)
                return "New Release";
            if (days < 365)
            {
                var months = (int)System.Math.Floor(days / 30.0);
                if (months <= 1) months = 1; // minimum 1 month when >=30 days
                return months + (months == 1 ? " month old" : " months old");
            }
            if (days < 1825)
            {
                var years = (int)System.Math.Floor(days / 365.0);
                if (years <= 1) years = 1;
                return years + (years == 1 ? " year old" : " years old");
            }
            // = 1825 → Classic (and beyond treated as Classic as well to satisfy equality case)
            return "Classic";
        }
    }
}

