using System.Threading.RateLimiting;
using VKProxy.Core.Infrastructure;
using VKProxy.Features.Limits;
using VKProxy.Middlewares.Http.Transforms;

namespace VKProxy.Config;

public class RouteConfig
{
    public int Order { get; set; }
    public string Key { get; set; }

    public string? ClusterId { get; set; }

    internal ClusterConfig ClusterConfig { get; set; }

    /// <summary>
    /// tcp : read / write timeout not connection timeout, udp revice response timeout, http ...
    /// </summary>
    public TimeSpan Timeout { get; set; }

    public int UdpResponses { get; set; }
    public RouteMatch? Match { get; set; }

    public IReadOnlyList<IReadOnlyDictionary<string, string>>? Transforms { get; set; }
    internal IHttpTransformer Transformer { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public ConcurrentConnectionLimitOptions Limit { get; set; }

    public IConnectionLimiter? ConnectionLimiter { get; set; }

    public static bool Equals(RouteConfig? t, RouteConfig? other)
    {
        if (other is null)
        {
            return t is null;
        }

        if (t is null)
        {
            return other is null;
        }

        return t.Order == other.Order
            && string.Equals(t.Key, other.Key, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.ClusterId, other.ClusterId, StringComparison.OrdinalIgnoreCase)
            && t.Timeout == other.Timeout
            && t.UdpResponses == other.UdpResponses
            && RouteMatch.Equals(t.Match, other.Match)
            && CollectionUtilities.Equals(t.Metadata, other.Metadata)
            && CollectionUtilities.Equals(t.Transforms, other.Transforms)
            && ConcurrentConnectionLimitOptions.Equals(t.Limit, other.Limit);
    }

    public override bool Equals(object? obj)
    {
        return obj is RouteConfig o && Equals(this, o);
    }

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(Order);
        code.Add(Key?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(ClusterId?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(Timeout.GetHashCode());
        code.Add(UdpResponses.GetHashCode());
        code.Add(Match?.GetHashCode());
        code.Add(CollectionUtilities.GetHashCode(Metadata));
        code.Add(CollectionUtilities.GetHashCode(Transforms));
        code.Add(Limit?.GetHashCode());
        return code.ToHashCode();
    }
}