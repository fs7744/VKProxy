using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace VKProxy.Core.Sockets.Udp;

internal class UdpReceiver : SocketAsyncEventArgs, IValueTaskSource<SocketReceiveFromResult>
{
    private static readonly Action<object?> _continuationCompleted = _ => { };

    private readonly PipeScheduler _ioScheduler;
    private volatile Action<object?>? _continuation;

    public UdpReceiver(PipeScheduler ioScheduler)
        : base(unsafeSuppressExecutionContextFlow: true)
    {
        _ioScheduler = ioScheduler;
    }

    protected override void OnCompleted(SocketAsyncEventArgs _)
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

    public SocketReceiveFromResult GetResult(short token)
    {
        _continuation = null;

        SocketError error = SocketError;
        int bytes = BytesTransferred;
        EndPoint remoteEndPoint = RemoteEndPoint!;

        if (error != SocketError.Success)
        {
            throw new SocketException((int)error);
        }

        return new SocketReceiveFromResult() { ReceivedBytes = bytes, RemoteEndPoint = remoteEndPoint };
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return !ReferenceEquals(_continuation, _continuationCompleted) ? ValueTaskSourceStatus.Pending :
                 SocketError == SocketError.Success ? ValueTaskSourceStatus.Succeeded :
                 ValueTaskSourceStatus.Faulted;
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        UserToken = state;
        var prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
        if (ReferenceEquals(prevContinuation, _continuationCompleted))
        {
            UserToken = null;
            ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
        }
    }

    public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Socket socket, Memory<byte> buffer)
    {
        SetBuffer(buffer);
        Debug.Assert(Volatile.Read(ref _continuation) is null, "Expected null continuation to indicate reserved for use");

        if (socket.ReceiveFromAsync(this))
        {
            return new ValueTask<SocketReceiveFromResult>(this, 0);
        }

        int bytesTransferred = BytesTransferred;
        EndPoint remoteEndPoint = RemoteEndPoint!;
        SocketError error = SocketError;

        return error == SocketError.Success ?
            new ValueTask<SocketReceiveFromResult>(new SocketReceiveFromResult() { ReceivedBytes = bytesTransferred, RemoteEndPoint = remoteEndPoint }) :
            ValueTask.FromException<SocketReceiveFromResult>(CreateException(error));
    }

    protected static SocketException CreateException(SocketError e)
    {
        return new SocketException((int)e);
    }
}