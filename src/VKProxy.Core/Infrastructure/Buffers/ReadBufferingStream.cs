using DotNext.Buffers;
using DotNext.IO;

namespace VKProxy.Core.Infrastructure.Buffers;

public class ReadBufferingStream : Stream, IDisposable
{
    private readonly SparseBufferWriter<byte> bufferWriter;
    protected Stream innerStream;

    public ReadBufferingStream(Stream innerStream)
    {
        this.innerStream = innerStream;
        bufferWriter = new SparseBufferWriter<byte>();
    }

    public override bool CanRead => innerStream.CanRead;

    public override bool CanSeek => innerStream.CanSeek;

    public override bool CanWrite => innerStream.CanWrite;

    public override long Length => innerStream.Length;

    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    public override int WriteTimeout
    {
        get => innerStream.WriteTimeout;
        set => innerStream.WriteTimeout = value;
    }

    public Stream BufferingStream => bufferWriter.WrittenCount > 0 ? bufferWriter.AsStream(true) : innerStream;

    public override void Flush()
    {
        innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return innerStream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var res = innerStream.Read(buffer, offset, count);

        // Zero-byte reads (where the passed in buffer has 0 length) can occur when using PipeReader, we don't want to accidentally complete the RequestBody logging in this case
        if (count == 0)
        {
            return res;
        }

        bufferWriter.Write(buffer.AsSpan(offset, res));

        return res;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var res = await innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        if (count == 0)
        {
            return res;
        }

        bufferWriter.Write(buffer.AsSpan(offset, res));

        return res;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        innerStream.Write(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return innerStream.WriteAsync(buffer, cancellationToken);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        innerStream.Write(buffer);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return innerStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return innerStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return innerStream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        innerStream.EndWrite(asyncResult);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var res = await innerStream.ReadAsync(buffer, cancellationToken);
        if (buffer.IsEmpty)
        {
            return res;
        }

        bufferWriter.Write(buffer.Slice(0, res).Span);

        return res;
    }

    public override ValueTask DisposeAsync()
    {
        return innerStream.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            bufferWriter.Dispose();
        }
    }
}