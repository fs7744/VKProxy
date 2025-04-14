namespace VKProxy.Config;

public sealed record DestinationConfig
{
    public static readonly IEqualityComparer<DestinationConfig> Comparer = EqualityComparer<DestinationConfig>.Create(Equals, GetHashCode);

    public string Address { get; init; } = default!;

    public string? Host { get; init; }

    public static bool Equals(DestinationConfig? t, DestinationConfig? other)
    {
        if (t is null && other is null) return true;
        if (other is null)
        {
            return false;
        }

        return string.Equals(t.Address, other.Address, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Host, other.Host, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(DestinationConfig? other)
    {
        return Equals(this, other);
    }

    public static int GetHashCode(DestinationConfig t)
    {
        return HashCode.Combine(
            t.Address?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            t.Host?.GetHashCode(StringComparison.OrdinalIgnoreCase));
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}