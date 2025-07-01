namespace VKProxy.Core.Infrastructure.AsyncObjectPool;

public class AsyncPooledObject<T> : IAsyncPooledObject<T> where T : IDisposable
{
    private readonly IAsyncObjectPool<IAsyncPooledObject<T>> pool;

    public AsyncPooledObject(IAsyncObjectPool<IAsyncPooledObject<T>> pool, T obj)
    {
        this.pool = pool;
        Obj = obj;
    }

    public T Obj { get; }

    public virtual void Dispose()
    {
        pool.Return(this);
    }

    public virtual ValueTask DisposeAsync()
    {
        return pool.ReturnAsync(this);
    }
}
