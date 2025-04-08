using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace VKProxy.Core.Sockets.Udp;

internal sealed class UdpReceiverPool : IDisposable
{
    private int _MaxQueueSize = 1024;

    private readonly ConcurrentQueue<UdpReceiver> _queue = new();
    private int _count;
    private readonly PipeScheduler _scheduler;
    private bool _disposed;

    public UdpReceiverPool(PipeScheduler scheduler, int poolSize = 1024)
    {
        _scheduler = scheduler;
        _MaxQueueSize = poolSize;
    }

    public PipeScheduler Scheduler => _scheduler;

    public UdpReceiver Rent()
    {
        if (_queue.TryDequeue(out var sender))
        {
            Interlocked.Decrement(ref _count);
            return sender;
        }
        return new UdpReceiver(_scheduler);
    }

    public void Return(UdpReceiver sender)
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