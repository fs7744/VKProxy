using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace VKProxy.Core.Sockets.Udp;

internal sealed class UdpSenderPool : IDisposable
{
    private int _MaxQueueSize;

    private readonly ConcurrentQueue<UdpSender> _queue = new();
    private int _count;
    private readonly PipeScheduler _scheduler;
    private bool _disposed;

    public UdpSenderPool(PipeScheduler scheduler, int poolSize = 1024)
    {
        _scheduler = scheduler;
        _MaxQueueSize = poolSize;
    }

    public PipeScheduler Scheduler => _scheduler;

    public UdpSender Rent()
    {
        if (_queue.TryDequeue(out var sender))
        {
            Interlocked.Decrement(ref _count);
            return sender;
        }
        return new UdpSender(_scheduler);
    }

    public void Return(UdpSender sender)
    {
        if (_disposed || Interlocked.Increment(ref _count) > _MaxQueueSize)
        {
            Interlocked.Decrement(ref _count);
            sender.Dispose();
            return;
        }

        sender.RemoteEndPoint = null;
        sender.SetBuffer(null, 0, 0);
        _queue.Enqueue(sender);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            while (_queue.TryDequeue(out var sender))
            {
                sender.Dispose();
            }
        }
    }
}