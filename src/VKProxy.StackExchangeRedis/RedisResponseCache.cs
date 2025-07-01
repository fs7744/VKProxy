using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

namespace VKProxy.StackExchangeRedis;

public class RedisResponseCache : IResponseCache
{
    private readonly IRedisPool pool;

    public string Name => "Redis";

    public RedisResponseCache(IRedisPool pool)
    {
        this.pool = pool;
    }

    public ValueTask<IResponseCacheEntry?> GetAsync(string key, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async ValueTask SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor, CancellationToken cancellationToken)
    {
        await using var redis = await pool.RentAsync();
        var db = redis.Obj.GetDatabase();
        throw new NotImplementedException();
    }
}