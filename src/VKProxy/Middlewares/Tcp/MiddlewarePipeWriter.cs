using Microsoft.AspNetCore.Connections;
using System.IO.Pipelines;

namespace VKProxy.Middlewares;

public class MiddlewarePipeWriter : PipeWriter
{
    private readonly PipeWriter pipeWriter;
    private readonly ConnectionContext context;
    private readonly TcpProxyDelegate connectionDelegate;

    public MiddlewarePipeWriter(PipeWriter pipeWriter, ConnectionContext context, TcpProxyDelegate connectionDelegate)
    {
        this.pipeWriter = pipeWriter;
        this.context = context;
        this.connectionDelegate = connectionDelegate;
    }

    public override void Advance(int bytes)
    {
        pipeWriter.Advance(bytes);
    }

    public override void CancelPendingFlush()
    {
        pipeWriter.CancelPendingFlush();
    }

    public override void Complete(Exception? exception = null)
    {
        pipeWriter.Complete(exception);
    }

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        return pipeWriter.FlushAsync(cancellationToken);
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        return pipeWriter.GetMemory(sizeHint);
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        return pipeWriter.GetSpan(sizeHint);
    }

    public override Stream AsStream(bool leaveOpen = false)
    {
        return pipeWriter.AsStream(leaveOpen);
    }

    public override bool CanGetUnflushedBytes => pipeWriter.CanGetUnflushedBytes;

    public override ValueTask CompleteAsync(Exception? exception = null)
    {
        return pipeWriter.CompleteAsync(exception);
    }

    public override void OnReaderCompleted(Action<Exception?, object?> callback, object? state)
    {
        pipeWriter.OnReaderCompleted(callback, state);
    }

    public override long UnflushedBytes => pipeWriter.UnflushedBytes;

    public override async ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        return await pipeWriter.WriteAsync(await connectionDelegate(context, source, cancellationToken), cancellationToken);
    }
}