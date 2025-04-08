using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace VKProxy.Core.Sockets.Udp;

internal class UdpSender : SocketAsyncEventArgs, IValueTaskSource<int>
{
    private static readonly Action<object?> _continuationCompleted = _ => { };

    private readonly PipeScheduler _ioScheduler;
    private volatile Action<object?>? _continuation;

    public UdpSender(PipeScheduler ioScheduler) : base(unsafeSuppressExecutionContextFlow: true)
    {
        _ioScheduler = ioScheduler;
    }

    public int GetResult(short token)
    {
        _continuation = null;

        if (SocketError != SocketError.Success)
        {
            throw CreateException(SocketError);
        }

        return BytesTransferred;
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return !ReferenceEquals(_continuation, _continuationCompleted) ? ValueTaskSourceStatus.Pending :
                 SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                 ValueTaskSourceStatus.Faulted;
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        var c = _continuation;

        if (c != null || (c = Interlocked.CompareExchange(ref _continuation, _continuationCompleted, null)) != null)
        {
            var continuationState = UserToken;
            UserToken = null;
            _continuation = _continuationCompleted; // in case someone's polling IsCompleted

            _ioScheduler.Schedule(c, continuationState);
        }
    }

    public ValueTask<int> SendToAsync(Socket socket, ReadOnlyMemory<byte> memory)
    {
        SetBuffer(MemoryMarshal.AsMemory(memory));

        if (socket.SendToAsync(this))
        {
            return new ValueTask<int>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success ?
                     new ValueTask<int>(bytesTransferred) :
                     ValueTask.FromException<int>(CreateException(error));
    }

    protected static SocketException CreateException(SocketError e)
    {
        return new SocketException((int)e);
    }
}