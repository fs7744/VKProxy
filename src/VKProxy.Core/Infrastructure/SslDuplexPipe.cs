using System.IO.Pipelines;
using System.Net.Security;

namespace VKProxy.Core.Infrastructure;

public sealed class SslDuplexPipe : DuplexPipeStreamAdapter<SslStream>
{
    public SslDuplexPipe(ReadResult? readResult, IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions)
        : this(readResult, transport, readerOptions, writerOptions, s => new SslStream(s))
    {
    }

    public SslDuplexPipe(ReadResult? readResult, IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, SslStream> factory) :
        base(readResult, transport, readerOptions, writerOptions, factory)
    {
    }
}