using System.Threading.RateLimiting;
using VKProxy.Core.Loggers;
using VKProxy.Features;
using VKProxy.Features.Limits;

namespace VKProxy.StackExchangeRedis;

public class RedisIncrConnectionLimitCreator : IConnectionLimitCreator
{
    private readonly IRedisPool pool;
    private readonly ProxyLogger logger;

    public string Name => "RedisConcurrency";

    public RedisIncrConnectionLimitCreator(IRedisPool pool, ProxyLogger logger)
    {
        this.pool = pool;
        this.logger = logger;
    }

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Header))
            return new RedisIncrConnectionLimiter(options, true, pool, logger);
        else if (!string.IsNullOrWhiteSpace(options.Cookie))
            return new RedisIncrConnectionLimiter(options, false, pool, logger);
        else
            return new RedisIncrConnectionLimiter(options, null, pool, logger);
    }
}

public class RedisIncrConnectionLimiter : IConnectionLimiter
{
    private ConcurrentConnectionLimitOptions options;
    private readonly bool? isHeader;
    private readonly IRedisPool pool;
    private readonly ProxyLogger logger;

    public RedisIncrConnectionLimiter(ConcurrentConnectionLimitOptions options, bool? isHeader, IRedisPool pool, ProxyLogger logger)
    {
        this.options = options;
        this.isHeader = isHeader;
        this.pool = pool;
        this.logger = logger;
    }

    public RateLimiter? GetLimiter(IReverseProxyFeature proxyFeature)
    {
        string key = isHeader.HasValue
            ? string.Concat(proxyFeature.Route?.Key ?? string.Empty, "#Concurrency#", ConnectionByKeyLimiter.GetKey(proxyFeature, options, isHeader.Value))
            : proxyFeature.Route?.Key ?? string.Empty;

        return new RedisIncrRateLimiter(key, pool, 1, logger);
    }

    public IEnumerable<KeyValuePair<string, RateLimiter>> GetAllLimiter()
    {
        return Array.Empty<KeyValuePair<string, RateLimiter>>();
    }
}