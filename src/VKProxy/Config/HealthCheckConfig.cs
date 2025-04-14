namespace VKProxy.Config;

public sealed record HealthCheckConfig
{
    public PassiveHealthCheckConfig? Passive { get; init; }

    public ActiveHealthCheckConfig? Active { get; init; }

    public static bool Equals(HealthCheckConfig? t, HealthCheckConfig? other)
    {
        if (t is null && other is null) return true;
        if (other is null)
        {
            return false;
        }

        return PassiveHealthCheckConfig.Equals(t.Passive, other.Passive)
            && ActiveHealthCheckConfig.Equals(t.Active, other.Active);
    }

    public bool Equals(HealthCheckConfig? other)
    {
        return Equals(this, other);
    }

    public static int GetHashCode(HealthCheckConfig t)
    {
        return HashCode.Combine(
            t.Passive,
            t.Active);
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}