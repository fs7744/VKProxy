using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public class MemoryResponseCache : IResponseCache
{
    private readonly IMemoryCache cache;

    public string Name => "Memory";

    public MemoryResponseCache(IMemoryCache cache)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public ValueTask<CachedResponse?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var entry = cache.Get(key);
        return ValueTask.FromResult(entry as CachedResponse);
    }

    public ValueTask SetAsync(string key, CachedResponse entry, TimeSpan validFor, CancellationToken cancellationToken)
    {
        cache.Set(
                key,
                entry,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = validFor,
                    Size = ResponseCacheFormatter.EstimateCachedResponseSize(entry)
                });
        return ValueTask.CompletedTask;
    }
}