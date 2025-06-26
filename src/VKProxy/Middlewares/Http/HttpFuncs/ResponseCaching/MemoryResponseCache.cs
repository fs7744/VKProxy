using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

internal class MemoryResponseCache : IResponseCache
{
    private readonly IMemoryCache cache;

    public string Name => "Memory";

    internal MemoryResponseCache(IMemoryCache cache)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public IResponseCacheEntry? Get(string key)
    {
        var entry = cache.Get(key);
        return entry as IResponseCacheEntry;
    }

    public void Set(string key, IResponseCacheEntry entry, TimeSpan validFor)
    {
        cache.Set(
                key,
                entry,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = validFor,
                    Size = EstimateCachedResponseSize(entry as CachedResponse)
                });
    }

    internal static long EstimateCachedResponseSize(CachedResponse cachedResponse)
    {
        if (cachedResponse == null)
        {
            return 0L;
        }

        checked
        {
            // StatusCode
            long size = sizeof(int);

            // Headers
            if (cachedResponse.Headers != null)
            {
                foreach (var item in cachedResponse.Headers)
                {
                    size += (item.Key.Length * sizeof(char)) + EstimateStringValuesSize(item.Value);
                }
            }

            // Body
            if (cachedResponse.Body != null)
            {
                size += cachedResponse.Body.Length;
            }

            return size;
        }
    }

    internal static long EstimateStringValuesSize(StringValues stringValues)
    {
        checked
        {
            var size = 0L;

            for (var i = 0; i < stringValues.Count; i++)
            {
                var stringValue = stringValues[i];
                if (!string.IsNullOrEmpty(stringValue))
                {
                    size += stringValue.Length * sizeof(char);
                }
            }

            return size;
        }
    }
}