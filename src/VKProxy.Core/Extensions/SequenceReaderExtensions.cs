using System.Buffers;
using System.Text;

namespace VKProxy.Core;

public static class SequenceReader
{
    private const int MaxStackLength = 128;

    public static string ReadString(this in ReadOnlySequence<byte> sequence, int length)
    {
        var reader = new SequenceReader<byte>(sequence);
        return ReadString(ref reader, length);
    }

    public static string ReadString(ref SequenceReader<byte> reader, int length)
    {
        string str;

        if (length < MaxStackLength)
        {
            Span<byte> byteBuffer = stackalloc byte[length];
            if (reader.TryCopyTo(byteBuffer))
            {
                reader.Advance(length);
                str = Encoding.UTF8.GetString(byteBuffer);
            }
            else
                str = string.Empty;
        }
        else
        {
            var byteBuffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                var span = byteBuffer.AsSpan().Slice(0, length);
                if (reader.TryCopyTo(span))
                {
                    reader.Advance(length);
                    str = Encoding.UTF8.GetString(span);
                }
                else
                    str = string.Empty;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }
        }

        return str;
    }
}