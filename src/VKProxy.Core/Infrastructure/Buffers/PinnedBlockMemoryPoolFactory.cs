using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace VKProxy.Core.Buffers;

/// todo remove when net10
public interface IMemoryPoolFactory<T>
{
    MemoryPool<T> Create();
}

public interface IMemoryPoolSizeFactory<T> : IMemoryPoolFactory<T>
{
    MemoryPool<T> Create(int blockSize);
}

public class PinnedBlockMemoryPoolFactory : IMemoryPoolSizeFactory<byte>, IAsyncDisposable
{
    private readonly IMeterFactory? _meterFactory;
    private readonly ConcurrentDictionary<PinnedBlockMemoryPool, bool> _pools = new();
    private readonly PeriodicTimer _timer;
    private readonly Task _timerTask;
    private readonly ILogger? _logger;
    private readonly MemoryPool<byte> shared;

    public PinnedBlockMemoryPoolFactory(IMeterFactory? meterFactory = null, ILogger<PinnedBlockMemoryPoolFactory>? logger = null)
    {
        _meterFactory = meterFactory;
        _logger = logger;
        _timer = new PeriodicTimer(PinnedBlockMemoryPool.DefaultEvictionDelay);
        shared = Create(4096);
        _timerTask = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync())
                {
                    foreach (var pool in _pools.Keys)
                    {
                        pool.PerformEviction();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Error while evicting memory from pools.");
            }
        });
    }

    public MemoryPool<byte> Create()
    {
        return shared;
    }

    public MemoryPool<byte> Create(int blockSize)
    {
        var pool = new PinnedBlockMemoryPool(blockSize, _meterFactory, _logger);

        _pools.TryAdd(pool, true);

        pool.OnPoolDisposed(static (state, self) =>
        {
            ((ConcurrentDictionary<PinnedBlockMemoryPool, bool>)state!).TryRemove(self, out _);
        }, _pools);

        return pool;
    }

    public async ValueTask DisposeAsync()
    {
        _timer.Dispose();
        await _timerTask;
    }
}