using DotNext.Buffers;
using Microsoft.Extensions.Logging;
using System.Buffers;
using VKProxy.Core.Loggers;
using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

namespace VKProxy.StackExchangeRedis;

public class RedisResponseCache : IResponseCache
{
    private readonly IRedisPool pool;
    private readonly ProxyLogger logger;

    public string Name => "Redis";

    public RedisResponseCache(IRedisPool pool, ProxyLogger logger)
    {
        this.pool = pool;
        this.logger = logger;
    }

    public async ValueTask<CachedResponse?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await using var redis = await pool.RentAsync();
            var db = redis.Obj.GetDatabase();
            byte[]? v = await db.StringGetAsync(key).ConfigureAwait(false);
            return v == null ? null : ResponseCacheFormatter.Deserialize(v);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async ValueTask SetAsync(string key, CachedResponse entry, TimeSpan validFor, CancellationToken cancellationToken)
    {
        try
        {
            await using var redis = await pool.RentAsync();
            var db = redis.Obj.GetDatabase();
            using var writer = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
            ResponseCacheFormatter.Serialize(writer, entry);
            await db.StringSetAsync(key, writer.WrittenMemory, validFor).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}