namespace VKProxy.Config;

public sealed record ActiveHealthCheckConfig
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    public string? Policy { get; set; } = "Connect";

    public int Passes { get; set; } = 1;

    public int Fails { get; set; } = 1;

    /// <summary>
    /// HTTP health check endpoint path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Query string to append to the probe, including the leading '?'.
    /// </summary>
    public string? Query { get; set; }

    public string? Method { get; set; }

    public static bool Equals(ActiveHealthCheckConfig? t, ActiveHealthCheckConfig? other)
    {
        if (t is null && other is null) return true;
        if (other is null)
        {
            return false;
        }

        return t.Interval == other.Interval
            && t.Timeout == other.Timeout
            && t.Passes == other.Passes
            && t.Fails == other.Fails
            && string.Equals(t.Policy, other.Policy, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Path, other.Path, StringComparison.Ordinal)
            && string.Equals(t.Query, other.Query, StringComparison.Ordinal)
            && string.Equals(t.Method, other.Method, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ActiveHealthCheckConfig? other)
    {
        return Equals(this, other);
    }

    public static int GetHashCode(ActiveHealthCheckConfig t)
    {
        return HashCode.Combine(t.Interval,
            t.Timeout,
            t.Passes,
            t.Fails,
            t.Policy?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            t.Path?.GetHashCode(StringComparison.Ordinal),
            t.Query?.GetHashCode(StringComparison.Ordinal),
            t.Method?.GetHashCode(StringComparison.OrdinalIgnoreCase));
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}