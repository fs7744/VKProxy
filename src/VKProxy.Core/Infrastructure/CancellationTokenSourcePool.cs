using System.Collections.Concurrent;

namespace VKProxy.Core.Infrastructure;

public sealed class CancellationTokenSourcePool
{
    public static readonly CancellationTokenSourcePool Default = new();
    private const int MaxQueueSize = 1024;

    private readonly ConcurrentQueue<PooledCancellationTokenSource> _queue = new();
    private int _count;

    public PooledCancellationTokenSource Rent()
    {
        if (_queue.TryDequeue(out var cts))
        {
            Interlocked.Decrement(ref _count);
            return cts;
        }
        return new PooledCancellationTokenSource(this);
    }

    private bool Return(PooledCancellationTokenSource cts)
    {
        if (Interlocked.Increment(ref _count) > MaxQueueSize || !cts.TryReset())
        {
            Interlocked.Decrement(ref _count);
            return false;
        }

        _queue.Enqueue(cts);
        return true;
    }

    public sealed class PooledCancellationTokenSource : CancellationTokenSource
    {
        private readonly CancellationTokenSourcePool _pool;

        public PooledCancellationTokenSource(CancellationTokenSourcePool pool)
        {
            _pool = pool;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_pool.Return(this))
                {
                    base.Dispose(disposing);
                }
            }
        }
    }
}