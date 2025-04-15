using VKProxy.Core.Sockets.Udp;

namespace VKProxy.Middlewares;

public interface IUdpProxyMiddleware
{
    Task InitAsync(UdpConnectionContext context, CancellationToken token, UdpDelegate next);

    Task<ReadOnlyMemory<byte>> OnRequestAsync(UdpConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, UdpProxyDelegate next);

    Task<ReadOnlyMemory<byte>> OnResponseAsync(UdpConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, UdpProxyDelegate next);
}

public delegate Task<ReadOnlyMemory<byte>> UdpProxyDelegate(UdpConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token);

public delegate Task UdpDelegate(UdpConnectionContext context, CancellationToken token);