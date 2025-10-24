using System.Threading;
using System.Threading.Tasks;

namespace ProductModule
{
    public interface ICacheService
    {
        Task RemoveAsync(string key, CancellationToken ct = default);
    }
}

