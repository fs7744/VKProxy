using Microsoft.Extensions.Configuration;
using VKProxy.Core.Config;

namespace VKProxy.Features.Limits;

public class ConcurrentConnectionLimitOptions
{
    public string? Policy { get; set; }
    public long? MaxConcurrentConnections { get; set; }

    public string? HttpIpHeader { get; set; }

    internal static ConcurrentConnectionLimitOptions Read(IConfigurationSection section)
    {
        if (!section.Exists()) return null;

        var limits = new ConcurrentConnectionLimitOptions
        {
            MaxConcurrentConnections = section.ReadInt64(nameof(ConcurrentConnectionLimitOptions.MaxConcurrentConnections)),
            Policy = section[nameof(ConcurrentConnectionLimitOptions.Policy)],
            HttpIpHeader = section[nameof(ConcurrentConnectionLimitOptions.HttpIpHeader)]
        };
        return limits;
    }

    public static bool Equals(ConcurrentConnectionLimitOptions? t, ConcurrentConnectionLimitOptions? other)
    {
        if (t is null && other is null) return true;
        if (other is null)
        {
            return false;
        }

        return t.MaxConcurrentConnections == other.MaxConcurrentConnections
            && string.Equals(t.Policy, other.Policy, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.HttpIpHeader, other.HttpIpHeader, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is ConcurrentConnectionLimitOptions o && Equals(this, o);
    }

    public static int GetHashCode(ConcurrentConnectionLimitOptions t)
    {
        return HashCode.Combine(t.MaxConcurrentConnections,
            t.Policy?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            t.HttpIpHeader?.GetHashCode(StringComparison.OrdinalIgnoreCase));
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}