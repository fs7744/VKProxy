using System.Collections.Frozen;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Config;

public sealed record RouteMatch
{
    /// <summary>
    /// tcp / udp will match instances ips:ports, http will match host header
    /// </summary>
    public IReadOnlyList<string>? Hosts { get; init; }

    public IReadOnlyList<string>? Paths { get; init; }

    public IReadOnlySet<string>? Methods { get; init; }

    public static bool Equals(RouteMatch? t, RouteMatch? other)
    {
        if (t is null && other is null) return true;
        if (other is null)
        {
            return false;
        }

        return CollectionUtilities.EqualsString(t.Hosts, other.Hosts)
            && CollectionUtilities.EqualsString(t.Paths, other.Paths)
            && CollectionUtilities.EqualsString(t.Methods, other.Methods);
    }

    public bool Equals(RouteMatch? other)
    {
        return Equals(this, other);
    }

    public static int GetHashCode(RouteMatch t)
    {
        return HashCode.Combine(CollectionUtilities.GetStringHashCode(t.Hosts), CollectionUtilities.GetStringHashCode(t.Paths), CollectionUtilities.GetStringHashCode(t.Methods));
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}