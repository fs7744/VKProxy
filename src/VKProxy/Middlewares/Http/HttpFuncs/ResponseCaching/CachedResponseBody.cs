using System.IO.Pipelines;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public interface ICachedResponseBody : IDisposable
{
    Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken);

    long Length { get; }
}

public sealed class CachedResponseBody : ICachedResponseBody
{
    public static readonly CachedResponseBody Empty = new CachedResponseBody(new List<byte[]>(0), 0);

    public CachedResponseBody(List<byte[]> segments, long length)
    {
        Segments = segments;
        Length = length;
    }

    public List<byte[]> Segments { get; }

    public long Length { get; }

    public async Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);

        foreach (var segment in Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Copy(segment, destination);

            await destination.FlushAsync(cancellationToken);
        }
    }

    private static void Copy(byte[] segment, PipeWriter destination)
    {
        var span = destination.GetSpan(segment.Length);

        segment.CopyTo(span);
        destination.Advance(segment.Length);
    }

    public void Dispose()
    {
    }
}

public class CachedStreamResponseBody : ICachedResponseBody
{
    public Stream Stream { get; private set; }

    public CachedStreamResponseBody(Stream stream, long length)
    {
        this.Stream = stream;
        Length = length;
    }

    public long Length { get; }

    public async Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken)
    {
        await Stream.CopyToAsync(destination, cancellationToken);
    }

    public void Dispose()
    {
        Stream.Dispose();
    }
}