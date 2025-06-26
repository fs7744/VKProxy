using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Collections.Frozen;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public class ResponseCachingFunc : IHttpFunc
{
    private readonly FrozenDictionary<string, IResponseCache> caches;
    private readonly TimeProvider timeProvider;
    private static readonly TimeSpan DefaultExpirationTimeSpan = TimeSpan.FromSeconds(10);

    // see https://tools.ietf.org/html/rfc7232#section-4.1
    private static readonly string[] HeadersToIncludeIn304 =
        new[] { "Cache-Control", "Content-Location", "Date", "ETag", "Expires", "Vary" };

    /// <summary>
    /// The segment size for buffering the response body in bytes. The default is set to 80 KB (81920 Bytes) to avoid allocations on the LOH.
    /// </summary>
    internal static int BodySegmentSize { get; set; } = 81920;

    public int Order => 10;

    public ResponseCachingFunc(IEnumerable<IResponseCache> caches, TimeProvider timeProvider)
    {
        this.caches = caches.ToFrozenDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        this.timeProvider = timeProvider;
    }

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        var (cc, cache, maximumBodySize) = GetCacheKeyFunc(config);
        if (cc == null || cache == null)
            return next;
        else return async c =>
        {
            if (AllowCache(c))
            {
                var key = cc(c);
                if (!string.IsNullOrEmpty(key))
                {
                    var context = new ResponseCachingContext(c) { Key = key, Cache = cache, MaximumBodySize = maximumBodySize };
                    if (await TryServeFromCacheAsync(cache.Get(context.Key), context))
                        return;

                    // Check request no-store
                    if (!HeaderUtilities.ContainsCacheDirective(context.HttpContext.Request.Headers.CacheControl, CacheControlHeaderValue.NoStoreString))
                    {
                        // Hook up to listen to the response stream
                        ShimResponseStream(context);

                        try
                        {
                            await next(c);

                            // If there was no response body, check the response headers now. We can cache things like redirects.
                            StartResponse(context);

                            // Finalize the cache entry
                            FinalizeCacheBody(context);
                        }
                        finally
                        {
                            // Unshim response stream
                            context.HttpContext.Response.Body = context.OriginalResponseStream;
                        }

                        return;
                    }
                }
            }
            await next(c);
        };
    }

    public bool IsResponseCacheable(ResponseCachingContext context)
    {
        var responseCacheControlHeader = context.HttpContext.Response.Headers.CacheControl;

        // Only cache pages explicitly marked with public
        if (!HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PublicString))
        {
            context.Logger.ResponseWithoutPublicNotCacheable();
            return false;
        }

        // Check response no-store
        if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoStoreString))
        {
            context.Logger.ResponseWithNoStoreNotCacheable();
            return false;
        }

        // Check no-cache
        if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoCacheString))
        {
            context.Logger.ResponseWithNoCacheNotCacheable();
            return false;
        }

        var response = context.HttpContext.Response;

        // Do not cache responses with Set-Cookie headers
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.Logger.ResponseWithSetCookieNotCacheable();
            return false;
        }

        // Do not cache responses varying by *
        var varyHeader = response.Headers.Vary;
        if (varyHeader.Count == 1 && string.Equals(varyHeader, "*", StringComparison.OrdinalIgnoreCase))
        {
            context.Logger.ResponseWithVaryStarNotCacheable();
            return false;
        }

        // Check private
        if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PrivateString))
        {
            context.Logger.ResponseWithPrivateNotCacheable();
            return false;
        }

        // Check response code
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.Logger.ResponseWithUnsuccessfulStatusCodeNotCacheable(response.StatusCode);
            return false;
        }

        // Check response freshness
        if (!context.ResponseDate.HasValue)
        {
            if (!context.ResponseSharedMaxAge.HasValue &&
                !context.ResponseMaxAge.HasValue &&
                context.ResponseTime!.Value >= context.ResponseExpires)
            {
                context.Logger.ExpirationExpiresExceeded(context.ResponseTime.Value, context.ResponseExpires.Value);
                return false;
            }
        }
        else
        {
            var age = context.ResponseTime!.Value - context.ResponseDate.Value;

            // Validate shared max age
            if (age >= context.ResponseSharedMaxAge)
            {
                context.Logger.ExpirationSharedMaxAgeExceeded(age, context.ResponseSharedMaxAge.Value);
                return false;
            }
            else if (!context.ResponseSharedMaxAge.HasValue)
            {
                // Validate max age
                if (age >= context.ResponseMaxAge)
                {
                    context.Logger.ExpirationMaxAgeExceeded(age, context.ResponseMaxAge.Value);
                    return false;
                }
                else if (!context.ResponseMaxAge.HasValue)
                {
                    // Validate expiration
                    if (context.ResponseTime.Value >= context.ResponseExpires)
                    {
                        context.Logger.ExpirationExpiresExceeded(context.ResponseTime.Value, context.ResponseExpires.Value);
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool OnFinalizeCacheHeaders(ResponseCachingContext context)
    {
        if (IsResponseCacheable(context))
        {
            var storeVaryByEntry = false;
            context.ShouldCacheResponse = true;

            // Create the cache entry now
            var response = context.HttpContext.Response;
            var headers = response.Headers;
            var varyHeaders = new StringValues(headers.GetCommaSeparatedValues(HeaderNames.Vary));
            var varyQueryKeys = new StringValues(context.HttpContext.Features.Get<IResponseCachingFeature>()?.VaryByQueryKeys);
            context.CachedResponseValidFor = context.ResponseSharedMaxAge ??
                context.ResponseMaxAge ??
                (context.ResponseExpires - context.ResponseTime!.Value) ??
                DefaultExpirationTimeSpan;

            // Ensure date header is set
            if (!context.ResponseDate.HasValue)
            {
                context.ResponseDate = context.ResponseTime!.Value;
                // Setting the date on the raw response headers.
                headers.Date = HeaderUtilities.FormatDate(context.ResponseDate.Value);
            }

            // Store the response on the state
            context.CachedResponse = new CachedResponse
            {
                Created = context.ResponseDate.Value,
                StatusCode = response.StatusCode,
                Headers = new HeaderDictionary()
            };

            foreach (var header in headers)
            {
                if (!string.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase))
                {
                    context.CachedResponse.Headers[header.Key] = header.Value;
                }
            }

            return storeVaryByEntry;
        }

        context.ResponseCachingStream.DisableBuffering();
        return false;
    }

    internal void FinalizeCacheHeaders(ResponseCachingContext context)
    {
        if (OnFinalizeCacheHeaders(context))
        {
            context.Cache.Set(context.Key, context.CachedResponse, context.CachedResponseValidFor);
        }
    }

    internal void FinalizeCacheBody(ResponseCachingContext context)
    {
        if (context.ShouldCacheResponse && context.ResponseCachingStream.BufferingEnabled)
        {
            var contentLength = context.HttpContext.Response.ContentLength;
            var cachedResponseBody = context.ResponseCachingStream.GetCachedResponseBody();
            if (!contentLength.HasValue || contentLength == cachedResponseBody.Length
                || (cachedResponseBody.Length == 0
                    && HttpMethods.IsHead(context.HttpContext.Request.Method)))
            {
                var response = context.HttpContext.Response;
                // Add a content-length if required
                if (!response.ContentLength.HasValue && StringValues.IsNullOrEmpty(response.Headers.TransferEncoding))
                {
                    context.CachedResponse.Headers.ContentLength = cachedResponseBody.Length;
                }

                context.CachedResponse.Body = cachedResponseBody;
                _logger.ResponseCached();
                context.Cache.Set(context.Key, context.CachedResponse, context.CachedResponseValidFor);
            }
            else
            {
                _logger.ResponseContentLengthMismatchNotCached();
            }
        }
        else
        {
            _logger.LogResponseNotCached();
        }
    }

    internal void StartResponse(ResponseCachingContext context)
    {
        if (OnStartResponse(context))
        {
            FinalizeCacheHeaders(context);
        }
    }

    private bool OnStartResponse(ResponseCachingContext context)
    {
        if (!context.ResponseStarted)
        {
            context.ResponseStarted = true;
            context.ResponseTime = timeProvider.GetUtcNow();

            return true;
        }
        return false;
    }

    private void ShimResponseStream(ResponseCachingContext context)
    {
        // Shim response stream
        context.OriginalResponseStream = context.HttpContext.Response.Body;
        context.ResponseCachingStream = new ResponseCachingStream(
            context.OriginalResponseStream,
            context.MaximumBodySize,
            BodySegmentSize,
            () => StartResponse(context));
        context.HttpContext.Response.Body = context.ResponseCachingStream;
    }

    private async Task<bool> TryServeFromCacheAsync(IResponseCacheEntry responseCacheEntry, ResponseCachingContext c)
    {
        if (await TryServeCachedResponseAsync(responseCacheEntry, c))
        {
            return true;
        }

        if (HeaderUtilities.ContainsCacheDirective(c.HttpContext.Request.Headers.CacheControl, CacheControlHeaderValue.OnlyIfCachedString))
        {
            _logger.GatewayTimeoutServed();
            c.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            return true;
        }
        _logger.NoResponseServed();
        return false;
    }

    private async Task<bool> TryServeCachedResponseAsync(IResponseCacheEntry? cacheEntry, ResponseCachingContext context)
    {
        if (!(cacheEntry is CachedResponse cachedResponse))
        {
            return false;
        }

        context.CachedResponse = cachedResponse;
        context.CachedResponseHeaders = cachedResponse.Headers;
        context.ResponseTime = timeProvider.GetUtcNow();
        var cachedEntryAge = context.ResponseTime.Value - context.CachedResponse.Created;
        context.CachedEntryAge = cachedEntryAge > TimeSpan.Zero ? cachedEntryAge : TimeSpan.Zero;

        if (IsCachedEntryFresh(context))
        {
            // Check conditional request rules
            if (ContentIsNotModified(context))
            {
                _logger.NotModifiedServed();
                context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;

                if (context.CachedResponseHeaders != null)
                {
                    foreach (var key in HeadersToIncludeIn304)
                    {
                        if (context.CachedResponseHeaders.TryGetValue(key, out var values))
                        {
                            context.HttpContext.Response.Headers[key] = values;
                        }
                    }
                }
            }
            else
            {
                var response = context.HttpContext.Response;
                // Copy the cached status code and response headers
                response.StatusCode = context.CachedResponse.StatusCode;
                foreach (var header in context.CachedResponse.Headers)
                {
                    response.Headers[header.Key] = header.Value;
                }

                // Note: int64 division truncates result and errors may be up to 1 second. This reduction in
                // accuracy of age calculation is considered appropriate since it is small compared to clock
                // skews and the "Age" header is an estimate of the real age of cached content.
                response.Headers.Age = HeaderUtilities.FormatNonNegativeInt64(context.CachedEntryAge.Value.Ticks / TimeSpan.TicksPerSecond);

                // Copy the cached response body
                var body = context.CachedResponse.Body;
                if (body.Length > 0)
                {
                    try
                    {
                        await body.CopyToAsync(response.BodyWriter, context.HttpContext.RequestAborted);
                    }
                    catch (OperationCanceledException)
                    {
                        context.HttpContext.Abort();
                    }
                }
                _logger.CachedResponseServed();
            }
            return true;
        }

        return false;
    }

    internal static bool ContentIsNotModified(ResponseCachingContext context)
    {
        var cachedResponseHeaders = context.CachedResponseHeaders;
        var ifNoneMatchHeader = context.HttpContext.Request.Headers.IfNoneMatch;

        if (!StringValues.IsNullOrEmpty(ifNoneMatchHeader))
        {
            if (ifNoneMatchHeader.Count == 1 && StringSegment.Equals(ifNoneMatchHeader[0], EntityTagHeaderValue.Any.Tag, StringComparison.OrdinalIgnoreCase))
            {
                context.Logger.NotModifiedIfNoneMatchStar();
                return true;
            }

            EntityTagHeaderValue? eTag;
            if (!StringValues.IsNullOrEmpty(cachedResponseHeaders.ETag)
                && EntityTagHeaderValue.TryParse(cachedResponseHeaders.ETag.ToString(), out eTag)
                && EntityTagHeaderValue.TryParseList(ifNoneMatchHeader, out var ifNoneMatchEtags))
            {
                for (var i = 0; i < ifNoneMatchEtags.Count; i++)
                {
                    var requestETag = ifNoneMatchEtags[i];
                    if (eTag.Compare(requestETag, useStrongComparison: false))
                    {
                        context.Logger.NotModifiedIfNoneMatchMatched(requestETag);
                        return true;
                    }
                }
            }
        }
        else
        {
            var ifModifiedSince = context.HttpContext.Request.Headers.IfModifiedSince;
            if (!StringValues.IsNullOrEmpty(ifModifiedSince))
            {
                DateTimeOffset modified;
                if (!HeaderUtilities.TryParseDate(cachedResponseHeaders.LastModified.ToString(), out modified) &&
                    !HeaderUtilities.TryParseDate(cachedResponseHeaders.Date.ToString(), out modified))
                {
                    return false;
                }

                DateTimeOffset modifiedSince;
                if (HeaderUtilities.TryParseDate(ifModifiedSince.ToString(), out modifiedSince) &&
                    modified <= modifiedSince)
                {
                    context.Logger.NotModifiedIfModifiedSinceSatisfied(modified, modifiedSince);
                    return true;
                }
            }
        }

        return false;
    }

    internal bool IsCachedEntryFresh(ResponseCachingContext context)
    {
        var age = context.CachedEntryAge!.Value;
        var cachedCacheControlHeaders = context.CachedResponseHeaders.CacheControl;
        var requestCacheControlHeaders = context.HttpContext.Request.Headers.CacheControl;

        // Add min-fresh requirements
        if (HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MinFreshString, out var minFresh))
        {
            age += minFresh.Value;
            context.Logger.ExpirationMinFreshAdded(minFresh.Value);
        }

        // Validate shared max age, this overrides any max age settings for shared caches
        TimeSpan? cachedSharedMaxAge;
        HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.SharedMaxAgeString, out cachedSharedMaxAge);

        if (age >= cachedSharedMaxAge)
        {
            // shared max age implies must revalidate
            context.Logger.ExpirationSharedMaxAgeExceeded(age, cachedSharedMaxAge.Value);
            return false;
        }
        else if (!cachedSharedMaxAge.HasValue)
        {
            TimeSpan? requestMaxAge;
            HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out requestMaxAge);

            TimeSpan? cachedMaxAge;
            HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out cachedMaxAge);

            var lowestMaxAge = cachedMaxAge < requestMaxAge ? cachedMaxAge : requestMaxAge ?? cachedMaxAge;
            // Validate max age
            if (age >= lowestMaxAge)
            {
                // Must revalidate or proxy revalidate
                if (HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.MustRevalidateString)
                    || HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.ProxyRevalidateString))
                {
                    context.Logger.ExpirationMustRevalidate(age, lowestMaxAge.Value);
                    return false;
                }

                TimeSpan? requestMaxStale;
                var maxStaleExist = HeaderUtilities.ContainsCacheDirective(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString);
                HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString, out requestMaxStale);

                // Request allows stale values with no age limit
                if (maxStaleExist && !requestMaxStale.HasValue)
                {
                    context.Logger.ExpirationInfiniteMaxStaleSatisfied(age, lowestMaxAge.Value);
                    return true;
                }

                // Request allows stale values with age limit
                if (requestMaxStale.HasValue && age - lowestMaxAge < requestMaxStale)
                {
                    context.Logger.ExpirationMaxStaleSatisfied(age, lowestMaxAge.Value, requestMaxStale.Value);
                    return true;
                }

                context.Logger.ExpirationMaxAgeExceeded(age, lowestMaxAge.Value);
                return false;
            }
            else if (!cachedMaxAge.HasValue && !requestMaxAge.HasValue)
            {
                // Validate expiration
                DateTimeOffset expires;
                if (HeaderUtilities.TryParseDate(context.CachedResponseHeaders.Expires.ToString(), out expires) &&
                    context.ResponseTime!.Value >= expires)
                {
                    context.Logger.ExpirationExpiresExceeded(context.ResponseTime.Value, expires);
                    return false;
                }
            }
        }

        return true;
    }

    private bool AllowCache(HttpContext c)
    {
        var request = c.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        var requestHeaders = request.Headers;

        if (!StringValues.IsNullOrEmpty(requestHeaders.Authorization))
        {
            return false;
        }

        var cacheControl = requestHeaders.CacheControl;
        if (!StringValues.IsNullOrEmpty(cacheControl))
        {
            if (HeaderUtilities.ContainsCacheDirective(cacheControl, CacheControlHeaderValue.NoCacheString))
            {
                return false;
            }
        }
        else
        {
            // Support for legacy HTTP 1.0 cache directive
            if (HeaderUtilities.ContainsCacheDirective(requestHeaders.Pragma, CacheControlHeaderValue.NoCacheString))
            {
                return false;
            }
        }

        return true;
    }

    private (Func<HttpContext, string>, IResponseCache, long) GetCacheKeyFunc(RouteConfig config)
    {
        if (config.Metadata == null
            || !config.Metadata.TryGetValue("Cache", out var c)
            || !caches.TryGetValue(c ?? "Memory", out var cache)) return (null, null, 0);
        long maximumBodySize = 64 * 1024 * 1024;
        if (config.Metadata.TryGetValue("CacheMaximumBodySize", out var s) && long.TryParse(s, out var sl) && sl > 0)
        {
            maximumBodySize = sl;
        }
        return (null, cache, maximumBodySize);
    }
}