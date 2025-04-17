﻿namespace VKProxy.Config;

public class RouteConfig
{
    public GatewayProtocols Protocols { get; set; }
    public int Order { get; set; }
    public string Key { get; set; }

    public string? ClusterId { get; set; }

    internal ClusterConfig ClusterConfig { get; set; }

    /// <summary>
    /// tcp : read / write timeout not connection timeout, udp revice response timeout, http ...
    /// </summary>
    public TimeSpan Timeout { get; set; }

    public int RetryCount { get; set; }
    public int UdpResponses { get; set; }
    public RouteMatch Match { get; set; }

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

        return t.Protocols == other.Protocols
            && t.Order == other.Order
            && string.Equals(t.Key, other.Key, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.ClusterId, other.ClusterId, StringComparison.OrdinalIgnoreCase)
            && t.Timeout == other.Timeout
            && t.RetryCount == other.RetryCount
            && t.UdpResponses == other.UdpResponses
            && RouteMatch.Equals(t.Match, other.Match);
    }

    public override bool Equals(object? obj)
    {
        return obj is RouteConfig o && Equals(this, o);
    }

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(Protocols);
        code.Add(Order);
        code.Add(Key?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(ClusterId?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(Timeout.GetHashCode());
        code.Add(RetryCount);
        code.Add(UdpResponses.GetHashCode());
        code.Add(Match?.GetHashCode());
        return code.ToHashCode();
    }
}