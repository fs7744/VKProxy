using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using VKProxy.Core.Loggers;

namespace VKProxy.StackExchangeRedis;

public class RedisIncrRateLimiter : RateLimiter
{
    private readonly RedisKey key;
    private readonly IRedisPool pool;
    private readonly int permitLimit;
    private readonly ProxyLogger logger;

    public RedisIncrRateLimiter(string key, IRedisPool pool, int permitLimit, ProxyLogger logger)
    {
        this.key = key;
        this.pool = pool;
        this.permitLimit = permitLimit;
        this.logger = logger;
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics()
    {
        return null;
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        await using var redis = await pool.RentAsync();
        var db = redis.Obj.GetDatabase();
        var total = await db.StringIncrementAsync(key, 0);
        if (total > permitLimit)
        {
            return null;
        }

        var _ = await db.StringIncrementAsync(key, permitCount);
        return new RedisIncrRateLimitLease(this, permitCount);
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        using var redis = pool.RentAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        var db = redis.Obj.GetDatabase();
        var total = db.StringIncrement(key, 0);
        if (total > permitLimit)
        {
            return null;
        }
        var _ = db.StringIncrement(key, permitCount);
        return new RedisIncrRateLimitLease(this, permitCount);
    }

    internal async ValueTask ReleaseAsync(int permitCount)
    {
        try
        {
            await using var redis = await pool.RentAsync();
            var db = redis.Obj.GetDatabase();
            var _ = await db.StringIncrementAsync(key, permitCount * -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}

public class RedisIncrRateLimitLease : RateLimitLease
{
    private bool _disposed;
    private RedisIncrRateLimiter redisIncrRateLimiter;
    private readonly int permitCount;

    public RedisIncrRateLimitLease(RedisIncrRateLimiter redisIncrRateLimiter, int permitCount)
    {
        this.redisIncrRateLimiter = redisIncrRateLimiter;
        this.permitCount = permitCount;
    }

    public override bool IsAcquired => true;

    public override IEnumerable<string> MetadataNames => null;

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        redisIncrRateLimiter.ReleaseAsync(permitCount).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}