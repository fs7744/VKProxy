using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.RateLimiting;

namespace VKProxy.Features.Limits;

public class ConnectionByKeyLimiter : IConnectionLimiter
{
    private ConcurrentConnectionLimitOptions options;
    private readonly bool isHeader;
    private readonly ConcurrentDictionary<string, RateLimiter> rateLimiters = new ConcurrentDictionary<string, RateLimiter>(StringComparer.OrdinalIgnoreCase);

    public ConnectionByKeyLimiter(ConcurrentConnectionLimitOptions options, bool isHeader)
    {
        this.options = options;
        this.isHeader = isHeader;
    }

    public RateLimiter? GetLimiter(IReverseProxyFeature proxyFeature)
    {
        string key = GetKey(proxyFeature, options, isHeader);

        return rateLimiters.GetOrAdd(key, CreateLimiter);
    }

    public static string GetKey(IReverseProxyFeature proxyFeature, ConcurrentConnectionLimitOptions options, bool isHeader)
    {
        string key;
        if (proxyFeature is IL7ReverseProxyFeature l7)
        {
            key = isHeader ? GetHeader(l7.Http, options.Header) : GetCookie(l7.Http, options.Cookie);
        }
        else if (proxyFeature is IL4ReverseProxyFeature l4 && l4.Connection.RemoteEndPoint is not null)
        {
            var e = l4.Connection.RemoteEndPoint;
            if (e is IPEndPoint ip)
            {
                key = ip.Address.ToString();
            }
            else
            {
                key = e.ToString();
            }
        }
        else
            key = null;

        if (key == null)
            key = string.Empty;
        return key;
    }

    public static string? GetCookie(HttpContext context, string cookie)
    {
        string r = context.Request.Cookies[cookie];
        if (r == null)
        {
            r = context.Connection.RemoteIpAddress?.ToString();
        }
        return r;
    }

    private RateLimiter? CreateLimiter(string key)
    {
        return ConnectionLimitByTotalCreator.CreateLimiter(options);
    }

    public static string? GetHeader(HttpContext context, string header)
    {
        string r = context.Request.Headers[header].FirstOrDefault();
        if (r == null)
        {
            r = context.Connection.RemoteIpAddress?.ToString();
        }
        else if (r.Contains(','))
        {
            r = r.Split(',', 2).First();
        }
        return r;
    }
}