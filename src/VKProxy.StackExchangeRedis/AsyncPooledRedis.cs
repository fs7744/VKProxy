using StackExchange.Redis;
using VKProxy.Core.Infrastructure.AsyncObjectPool;

namespace VKProxy.StackExchangeRedis;

public class AsyncPooledRedis : AsyncPooledObject<IConnectionMultiplexer>
{
    public AsyncPooledRedis(IAsyncObjectPool<IAsyncPooledObject<IConnectionMultiplexer>> pool, IConnectionMultiplexer obj) : base(pool, obj)
    {
    }

    public override void Dispose()
    {
        if (Obj.IsConnected)
        {
            base.Dispose();
        }
        else
        {
            Obj.Dispose();
        }
    }

    public override ValueTask DisposeAsync()
    {
        if (Obj.IsConnected)
        {
            return base.DisposeAsync();
        }
        else
        {
            return Obj.DisposeAsync();
        }
    }
}