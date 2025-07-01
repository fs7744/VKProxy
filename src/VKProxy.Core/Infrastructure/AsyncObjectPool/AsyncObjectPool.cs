using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;

namespace VKProxy.Core.Infrastructure.AsyncObjectPool;

public class AsyncObjectPool<T> : IAsyncObjectPool<T> where T : IDisposable
{
    private readonly ConcurrentQueue<T> _queue = new();
    private int _count;
    private bool _disposed;
    private int _MaxQueueSize;
    private Func<IAsyncObjectPool<T>, Task<T>> func;

    public AsyncObjectPool(Func<IAsyncObjectPool<T>, Task<T>> func, int maxSize)
    {
        this.func = func;
        _MaxQueueSize = maxSize;
    }

    public void Dispose()
    {
        DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            while (_queue.TryDequeue(out var obj))
            {
                if (obj is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync();
                }
                else
                    obj.Dispose();
            }
        }
    }

    public virtual T Rent()
    {
        return RentAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<T> RentAsync()
    {
        if (_queue.TryDequeue(out var obj))
        {
            Interlocked.Decrement(ref _count);
            return obj;
        }
        return await func(this);
    }

    public void Return(T obj)
    {
        if ((obj is IResettable r && !r.TryReset()) || _disposed || Interlocked.Increment(ref _count) > _MaxQueueSize)
        {
            Interlocked.Decrement(ref _count);
            obj.Dispose();
            return;
        }

        _queue.Enqueue(obj);
    }

    public async ValueTask ReturnAsync(T obj)
    {
        if ((obj is IResettable r && !r.TryReset()) || _disposed || Interlocked.Increment(ref _count) > _MaxQueueSize)
        {
            Interlocked.Decrement(ref _count);
            if (obj is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
            else
                obj.Dispose();
            return;
        }

        _queue.Enqueue(obj);
    }
}