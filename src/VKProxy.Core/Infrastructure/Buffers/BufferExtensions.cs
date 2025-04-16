using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VKProxy.Core.Buffers;

public static class BufferExtensions
{
    public static int GetMinimumSegmentSize(this MemoryPool<byte> pool)
    {
        if (pool == null)
        {
            return 4096;
        }

        return Math.Min(4096, pool.MaxBufferSize);
    }

    public static int GetMinimumAllocSize(this MemoryPool<byte> pool)
    {
        // 1/2 of a segment
        return pool.GetMinimumSegmentSize() / 2;
    }

    public static ArraySegment<byte> GetArray(this Memory<byte> memory)
    {
        return ((ReadOnlyMemory<byte>)memory).GetArray();
    }

    public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory)
    {
        if (!MemoryMarshal.TryGetArray(memory, out var result))
        {
            throw new InvalidOperationException("Buffer backed by array was expected");
        }
        return result;
    }

    public static async Task CopyToAsync(this ReadResult result, PipeWriter writer, CancellationToken cancellationToken = default)
    {
        foreach (var item in result.Buffer)
        {
            var f = await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
            if (f.IsCompleted)
            {
                return;
            }
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> ToSpan(in this ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            return buffer.FirstSpan;
        }
        return buffer.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(in this ReadOnlySequence<byte> buffer, PipeWriter pipeWriter)
    {
        if (buffer.IsSingleSegment)
        {
            pipeWriter.Write(buffer.FirstSpan);
        }
        else
        {
            CopyToMultiSegment(buffer, pipeWriter);
        }
    }

    private static void CopyToMultiSegment(in ReadOnlySequence<byte> buffer, PipeWriter pipeWriter)
    {
        foreach (var item in buffer)
        {
            pipeWriter.Write(item.Span);
        }
    }
}