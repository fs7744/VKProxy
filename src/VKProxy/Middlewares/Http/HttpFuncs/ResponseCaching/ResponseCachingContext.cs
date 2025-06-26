using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

internal class ResponseCachingContext
{
    private DateTimeOffset? _responseDate;
    private bool _parsedResponseDate;
    private DateTimeOffset? _responseExpires;
    private bool _parsedResponseExpires;
    private TimeSpan? _responseSharedMaxAge;
    private bool _parsedResponseSharedMaxAge;
    private TimeSpan? _responseMaxAge;
    private bool _parsedResponseMaxAge;

    internal ResponseCachingContext(HttpContext httpContext)
    {
        HttpContext = httpContext;
    }

    public HttpContext HttpContext { get; }

    public DateTimeOffset? ResponseTime { get; internal set; }

    public TimeSpan? CachedEntryAge { get; internal set; }
    public IResponseCache Cache { get; internal set; }
    public long MaximumBodySize { get; internal set; }
    internal ILogger Logger { get; }

    internal bool ShouldCacheResponse { get; set; }

    internal string Key { get; set; }

    internal TimeSpan CachedResponseValidFor { get; set; }

    internal CachedResponse CachedResponse { get; set; }

    internal bool ResponseStarted { get; set; }

    internal Stream OriginalResponseStream { get; set; }

    internal ResponseCachingStream ResponseCachingStream { get; set; }

    internal IHeaderDictionary CachedResponseHeaders { get; set; }

    internal DateTimeOffset? ResponseDate
    {
        get
        {
            if (!_parsedResponseDate)
            {
                _parsedResponseDate = true;
                DateTimeOffset date;
                if (HeaderUtilities.TryParseDate(HttpContext.Response.Headers.Date.ToString(), out date))
                {
                    _responseDate = date;
                }
                else
                {
                    _responseDate = null;
                }
            }
            return _responseDate;
        }
        set
        {
            // Don't reparse the response date again if it's explicitly set
            _parsedResponseDate = true;
            _responseDate = value;
        }
    }

    internal DateTimeOffset? ResponseExpires
    {
        get
        {
            if (!_parsedResponseExpires)
            {
                _parsedResponseExpires = true;
                DateTimeOffset expires;
                if (HeaderUtilities.TryParseDate(HttpContext.Response.Headers.Expires.ToString(), out expires))
                {
                    _responseExpires = expires;
                }
                else
                {
                    _responseExpires = null;
                }
            }
            return _responseExpires;
        }
    }

    internal TimeSpan? ResponseSharedMaxAge
    {
        get
        {
            if (!_parsedResponseSharedMaxAge)
            {
                _parsedResponseSharedMaxAge = true;
                HeaderUtilities.TryParseSeconds(HttpContext.Response.Headers.CacheControl, CacheControlHeaderValue.SharedMaxAgeString, out _responseSharedMaxAge);
            }
            return _responseSharedMaxAge;
        }
    }

    internal TimeSpan? ResponseMaxAge
    {
        get
        {
            if (!_parsedResponseMaxAge)
            {
                _parsedResponseMaxAge = true;
                HeaderUtilities.TryParseSeconds(HttpContext.Response.Headers.CacheControl, CacheControlHeaderValue.MaxAgeString, out _responseMaxAge);
            }
            return _responseMaxAge;
        }
    }
}