using System.Threading.RateLimiting;

namespace VKProxy.Features.Limits;

public class ConnectionLimitByTotalCreator : IConnectionLimitCreator
{
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(10);
    public string Name => "Total";

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options)
    {
        var r = CreateLimiter(options);
        return r == null ? null : new ConnectionLimiter(r);
    }

    public static RateLimiter? CreateLimiter(ConcurrentConnectionLimitOptions options)
    {
        if (options == null || string.IsNullOrWhiteSpace(options.Policy)) return null;
        else if ("Concurrency".Equals(options.Policy, StringComparison.OrdinalIgnoreCase))
        {
            return options.PermitLimit.HasValue && options.PermitLimit.Value > 0
                ? new ConcurrencyLimiter(new ConcurrencyLimiterOptions() { PermitLimit = 1, QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = options.QueueLimit.GetValueOrDefault() })
                : null;
        }
        else if ("FixedWindow".Equals(options.Policy, StringComparison.OrdinalIgnoreCase))
        {
            return options.PermitLimit.HasValue && options.PermitLimit.Value > 0
                ? new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions()
                {
                    PermitLimit = options.PermitLimit.Value,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit.GetValueOrDefault(),
                    AutoReplenishment = true,
                    Window = options.Window.GetValueOrDefault(DefaultWindow)
                })
                : null;
        }
        else if ("SlidingWindow".Equals(options.Policy, StringComparison.OrdinalIgnoreCase))
        {
            return options.PermitLimit.HasValue && options.PermitLimit.Value > 0
                ? new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions()
                {
                    PermitLimit = options.PermitLimit.Value,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit.GetValueOrDefault(),
                    AutoReplenishment = true,
                    Window = options.Window.GetValueOrDefault(DefaultWindow),
                    SegmentsPerWindow = options.SegmentsPerWindow.GetValueOrDefault(2)
                })
                : null;
        }
        else if ("TokenBucket".Equals(options.Policy, StringComparison.OrdinalIgnoreCase))
        {
            return options.PermitLimit.HasValue && options.PermitLimit.Value > 0
                ? new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions()
                {
                    TokenLimit = options.PermitLimit.Value,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit.GetValueOrDefault(),
                    AutoReplenishment = true,
                    ReplenishmentPeriod = options.Window.GetValueOrDefault(DefaultWindow),
                    TokensPerPeriod = options.TokensPerPeriod.GetValueOrDefault(options.PermitLimit.Value)
                })
                : null;
        }
        else
            return null;
    }
}