﻿using VKProxy.Core.Infrastructure;

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
        if (other is null)
        {
            return t is null;
        }

        if (t is null)
        {
            return other is null;
        }

        return CollectionUtilities.EqualsString(t.Hosts, other.Hosts)
            && CollectionUtilities.EqualsString(t.Paths, other.Paths)
            && CollectionUtilities.EqualsString(t.Methods, other.Methods);
    }

    public bool Equals(RouteMatch? obj)
    {
        return obj is RouteMatch o && Equals(this, o);
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