using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Features.Limits;

public class ConnectionIpLimiter : IConnectionLimiter
{
    private readonly ConcurrentDictionary<string, ResourceCounter> concurrentConnectionCounter = new ConcurrentDictionary<string, ResourceCounter>(StringComparer.OrdinalIgnoreCase);
    private readonly long max;
    private readonly string header;

    public ConnectionIpLimiter(long max, string header)
    {
        this.max = max;
        this.header = header;
    }

    public IDecrementConcurrentConnectionCountFeature? TryLockOne(HttpContext context)
    {
        var ip = GetHttpIp(context) ?? string.Empty;
        var c = concurrentConnectionCounter.GetOrAdd(ip, CreateConnectionIpLimiter);
        if (c.TryLockOne())
            return new ConnectionReleasor(c);
        else
            return null;
    }

    private string? GetHttpIp(HttpContext context)
    {
        string r;
        if (string.IsNullOrWhiteSpace(header))
            r = context.Connection.RemoteIpAddress?.ToString();
        else
        {
            r = context.Request.Headers[header].FirstOrDefault();
            if (r != null && r.Contains(','))
            {
                r = r.Split(',', 2).First();
            }
        }

        return r;
    }

    public IDecrementConcurrentConnectionCountFeature? TryLockOne(ConnectionContext connection)
    {
        var ip = connection.RemoteEndPoint is IPEndPoint i ? i.Address.ToString() : string.Empty;
        var c = concurrentConnectionCounter.GetOrAdd(ip, CreateConnectionIpLimiter);
        if (c.TryLockOne())
            return new ConnectionReleasor(c);
        else
            return null;
    }

    private ResourceCounter CreateConnectionIpLimiter(string key)
    {
        return ResourceCounter.Quota(max);
    }
}