using StackExchange.Redis;
using VKProxy.Core.Infrastructure.AsyncObjectPool;

namespace VKProxy.StackExchangeRedis;

public class RedisPool : AsyncObjectPool<IAsyncPooledObject<IConnectionMultiplexer>>, IRedisPool
{
    public RedisPool(Func<IAsyncObjectPool<IAsyncPooledObject<IConnectionMultiplexer>>, Task<IAsyncPooledObject<IConnectionMultiplexer>>> func, int maxSize) : base(func, maxSize)
    {
    }
}