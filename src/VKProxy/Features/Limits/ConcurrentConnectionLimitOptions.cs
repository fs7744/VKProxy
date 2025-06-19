using Microsoft.Extensions.Configuration;
using VKProxy.Core.Config;

namespace VKProxy.Features.Limits;

public class ConcurrentConnectionLimitOptions
{
    public string? Policy { get; set; } // TokenBucket / Concurrency / FixedWindow / SlidingWindow
    public string? By { get; set; }  // Total / Key
    public int? PermitLimit { get; set; }
    public int? QueueLimit { get; set; }
    public int? SegmentsPerWindow { get; set; }
    public int? TokensPerPeriod { get; set; }
    public string? Header { get; set; }
    public TimeSpan? Window { get; set; }
    public string? Cookie { get; set; }

    internal static ConcurrentConnectionLimitOptions Read(IConfigurationSection section)
    {
        if (!section.Exists()) return null;

        var limits = new ConcurrentConnectionLimitOptions
        {
            PermitLimit = section.ReadInt32(nameof(ConcurrentConnectionLimitOptions.PermitLimit)),
            QueueLimit = section.ReadInt32(nameof(ConcurrentConnectionLimitOptions.QueueLimit)),
            SegmentsPerWindow = section.ReadInt32(nameof(ConcurrentConnectionLimitOptions.SegmentsPerWindow)),
            TokensPerPeriod = section.ReadInt32(nameof(ConcurrentConnectionLimitOptions.TokensPerPeriod)),
            Policy = section[nameof(ConcurrentConnectionLimitOptions.Policy)],
            Cookie = section[nameof(ConcurrentConnectionLimitOptions.Cookie)],
            By = section[nameof(ConcurrentConnectionLimitOptions.By)],
            Header = section[nameof(ConcurrentConnectionLimitOptions.Header)],
            Window = section.ReadTimeSpan(nameof(ConcurrentConnectionLimitOptions.Window)),
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

        return t.PermitLimit == other.PermitLimit
            && t.QueueLimit == other.QueueLimit
            && t.SegmentsPerWindow == other.SegmentsPerWindow
            && t.TokensPerPeriod == other.TokensPerPeriod
            && string.Equals(t.Policy, other.Policy, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Header, other.Header, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.By, other.By, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Cookie, other.Cookie, StringComparison.OrdinalIgnoreCase)
            && t.Window == other.Window;
    }

    public override bool Equals(object? obj)
    {
        return obj is ConcurrentConnectionLimitOptions o && Equals(this, o);
    }

    public static int GetHashCode(ConcurrentConnectionLimitOptions t)
    {
        var code = new HashCode();
        code.Add(t.PermitLimit);
        code.Add(t.QueueLimit);
        code.Add(t.SegmentsPerWindow);
        code.Add(t.TokensPerPeriod);
        code.Add(t.Policy?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(t.Header?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(t.Cookie?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(t.By?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        code.Add(t.Window);
        return code.ToHashCode();
    }

    public override int GetHashCode()
    {
        return GetHashCode(this);
    }
}