using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Buffers;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Text;
using VKProxy.Core.Buffers;
using VKProxy.Core.Infrastructure;
using VKProxy.Features;
using VKProxy.Middlewares.Http;

namespace VKProxy.Middlewares.Socks5;

internal class WSToSocks5HttpMiddleware : IMiddleware
{
    private static ReadOnlySpan<byte> EncodedWebSocketKey => "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"u8;
    private WebSocketMiddleware middleware;
    private readonly Socks5Middleware socks5Middleware;

    public WSToSocks5HttpMiddleware(IOptions<WebSocketOptions> options, ILoggerFactory loggerFactory, Socks5Middleware socks5Middleware)
    {
        middleware = new WebSocketMiddleware(Scoks5, options, loggerFactory);
        this.socks5Middleware = socks5Middleware;
    }

    private async Task Scoks5(HttpContext context)
    {
        var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
        var f = context.Features.Get<IHttpWebSocketFeature>();
        if (f.IsWebSocketRequest)
        {
            var responseHeaders = context.Response.Headers;
            responseHeaders.Connection = HeaderNames.Upgrade;
            responseHeaders.Upgrade = HttpForwarder.WebSocketName;
            responseHeaders.SecWebSocketAccept = CreateResponseKey(context.Request.Headers.SecWebSocketKey.ToString());

            var stream = await upgradeFeature!.UpgradeAsync(); // Sets status code to 101

            var memoryPool = context is IMemoryPoolFeature s ? s.MemoryPool : MemoryPool<byte>.Shared;
            StreamPipeReaderOptions readerOptions = new StreamPipeReaderOptions
            (
                pool: memoryPool,
                bufferSize: memoryPool.GetMinimumSegmentSize(),
                minimumReadSize: memoryPool.GetMinimumAllocSize(),
                leaveOpen: true,
                useZeroByteReads: true
            );

            var writerOptions = new StreamPipeWriterOptions
            (
                pool: memoryPool,
                leaveOpen: true
            );

            var input = PipeReader.Create(stream, readerOptions);
            var output = PipeWriter.Create(stream, writerOptions);
            var feature = context.Features.Get<IReverseProxyFeature>();
            var route = feature.Route;
            using var cts = CancellationTokenSourcePool.Default.Rent(route.Timeout.Value);
            var token = cts.Token;
            context.Features.Set<IL4ReverseProxyFeature>(new L4ReverseProxyFeature() { IsDone = true, Route = route });
            await socks5Middleware.Proxy(new WebSocketConnection(context.Features)
            {
                Transport = new WebSocketDuplexPipe() { Input = input, Output = output },
                ConnectionId = context.Connection.Id,
                Items = context.Items,
            }, null, token);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    public static string CreateResponseKey(string requestKey)
    {
        // "The value of this header field is constructed by concatenating /key/, defined above in step 4
        // in Section 4.2.2, with the string "258EAFA5-E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
        // this concatenated value to obtain a 20-byte value and base64-encoding"
        // https://tools.ietf.org/html/rfc6455#section-4.2.2

        // requestKey is already verified to be small (24 bytes) by 'IsRequestKeyValid()' and everything is 1:1 mapping to UTF8 bytes
        // so this can be hardcoded to 60 bytes for the requestKey + static websocket string
        Span<byte> mergedBytes = stackalloc byte[60];
        Encoding.UTF8.GetBytes(requestKey, mergedBytes);
        EncodedWebSocketKey.CopyTo(mergedBytes[24..]);

        Span<byte> hashedBytes = stackalloc byte[20];
        var written = SHA1.HashData(mergedBytes, hashedBytes);
        if (written != 20)
        {
            throw new InvalidOperationException("Could not compute the hash for the 'Sec-WebSocket-Accept' header.");
        }

        return Convert.ToBase64String(hashedBytes);
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var feature = context.Features.Get<IReverseProxyFeature>();
        if (feature is not null)
        {
            var route = feature.Route;
            if (route is not null && route.Metadata is not null
                && route.Metadata.TryGetValue("WSToSocks5", out var b) && bool.TryParse(b, out var isSocks5) && isSocks5)
            {
                return middleware.Invoke(context);
            }
        }
        return next(context);
    }
}

internal class WebSocketConnection : ConnectionContext
{
    public WebSocketConnection(IFeatureCollection features)
    {
        this.features = features;
    }

    public override IDuplexPipe Transport { get; set; }
    public override string ConnectionId { get; set; }

    private IFeatureCollection features;
    public override IFeatureCollection Features => features;

    public override IDictionary<object, object?> Items { get; set; }
}

internal class WebSocketDuplexPipe : IDuplexPipe
{
    public PipeReader Input { get; set; }

    public PipeWriter Output { get; set; }
}