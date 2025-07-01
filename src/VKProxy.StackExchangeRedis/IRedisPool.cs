using StackExchange.Redis;
using VKProxy.Core.Infrastructure.AsyncObjectPool;

namespace VKProxy.StackExchangeRedis;

public interface IRedisPool : IAsyncObjectPool<IAsyncPooledObject<IConnectionMultiplexer>>
{
}
