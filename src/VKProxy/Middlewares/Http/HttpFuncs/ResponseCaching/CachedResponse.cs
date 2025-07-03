using Microsoft.AspNetCore.Http;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public sealed class CachedResponse : IResponseCacheEntry, IDisposable
{
    public DateTimeOffset Created { get; set; }

    public int StatusCode { get; set; }

    public IHeaderDictionary Headers { get; set; } = default!;

    public ICachedResponseBody Body { get; set; } = default!;

    public void Dispose()
    {
        Body?.Dispose();
    }
}