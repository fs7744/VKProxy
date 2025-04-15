using VKProxy.Core.Infrastructure;

namespace VKProxy.Config;

public sealed record RouteMatch
{
    /// <summary>
    /// tcp / udp will match instances ips:ports, http will match host header
    /// </summary>
    public IReadOnlyList<string>? Hosts { get; init; }

    public static bool Equals(RouteMatch? t, RouteMatch? other)
    {
        if (t is null && other is null) return true;
        if (other is null)
        {
            return false;
        }

        return CollectionUtilities.Equals(t.Hosts, other.Hosts);
    }

    public bool Equals(RouteMatch? other)
    {
        return Equals(this, other);
    }

    public static int GetHashCode(RouteMatch t)
    {
        return HashCode.Combine(CollectionUtilities.GetStringHashCode(t.Hosts));
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}