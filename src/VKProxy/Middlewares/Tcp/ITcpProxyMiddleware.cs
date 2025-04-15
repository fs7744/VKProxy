using Microsoft.AspNetCore.Connections;

namespace VKProxy.Middlewares;

public interface ITcpProxyMiddleware
{
    Task InitAsync(ConnectionContext context, CancellationToken token, TcpDelegate next);

    Task<ReadOnlyMemory<byte>> OnRequestAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next);

    Task<ReadOnlyMemory<byte>> OnResponseAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next);
}

public delegate Task<ReadOnlyMemory<byte>> TcpProxyDelegate(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token);

public delegate Task TcpDelegate(ConnectionContext context, CancellationToken token);