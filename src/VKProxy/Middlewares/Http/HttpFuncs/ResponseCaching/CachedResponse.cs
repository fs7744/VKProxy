using Microsoft.AspNetCore.Http;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public sealed class CachedResponse : IResponseCacheEntry
{
    public DateTimeOffset Created { get; set; }

    public int StatusCode { get; set; }

    public IHeaderDictionary Headers { get; set; } = default!;

    public CachedResponseBody Body { get; set; } = default!;
}