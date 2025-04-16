namespace VKProxy.Config;

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

    public bool Equals(RouteConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        return Protocols == other.Protocols
            && Order == other.Order
            && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ClusterId, other.ClusterId, StringComparison.OrdinalIgnoreCase)
            && Timeout == other.Timeout
            && RetryCount == other.RetryCount
            && UdpResponses == other.UdpResponses
            && Match == other.Match;
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
        code.Add(Match.GetHashCode());
        return code.ToHashCode();
    }
}