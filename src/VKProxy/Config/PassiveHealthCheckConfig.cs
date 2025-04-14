namespace VKProxy.Config;

public sealed record PassiveHealthCheckConfig
{
    public TimeSpan DetectionWindowSize { get; set; } = TimeSpan.FromSeconds(60);
    public int MinimalTotalCountThreshold { get; set; } = 10;
    public double FailureRateLimit { get; set; } = 0.3;
    public TimeSpan ReactivationPeriod { get; set; } = TimeSpan.FromSeconds(60);

    public static bool Equals(PassiveHealthCheckConfig? t, PassiveHealthCheckConfig? other)
    {
        if (t is null && other is null) return true;
        if (other is null)
        {
            return false;
        }

        return t.DetectionWindowSize == other.DetectionWindowSize
            && t.MinimalTotalCountThreshold == other.MinimalTotalCountThreshold
            && t.FailureRateLimit == other.FailureRateLimit
            && t.ReactivationPeriod == other.ReactivationPeriod;
    }

    public bool Equals(PassiveHealthCheckConfig? other)
    {
        return Equals(this, other);
    }

    public static int GetHashCode(PassiveHealthCheckConfig t)
    {
        return HashCode.Combine(
            t.DetectionWindowSize,
            t.MinimalTotalCountThreshold,
            t.FailureRateLimit,
            t.ReactivationPeriod);
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}