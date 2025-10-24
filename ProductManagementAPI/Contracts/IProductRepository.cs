using System.Threading;
using System.Threading.Tasks;

namespace ProductModule
{
    public interface IProductRepository
    {
        Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
        Task AddAsync(Product product, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

