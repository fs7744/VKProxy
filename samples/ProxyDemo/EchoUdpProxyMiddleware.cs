using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Features;
using VKProxy.Middlewares;

namespace ProxyDemo;

internal class EchoUdpProxyMiddleware : IUdpProxyMiddleware
{
    private readonly ILogger<EchoUdpProxyMiddleware> logger;

    public EchoUdpProxyMiddleware(ILogger<EchoUdpProxyMiddleware> logger)
    {
        this.logger = logger;
    }

    public Task InitAsync(UdpConnectionContext context, CancellationToken token, UdpDelegate next)
    {
        return next(context, token);
    }

    public Task<ReadOnlyMemory<byte>> OnRequestAsync(UdpConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, UdpProxyDelegate next)
    {
        logger.LogInformation($"udp {DateTime.Now} {context.LocalEndPoint.ToString()} request size: {source.Length}");
        return next(context, source, token);
    }

    public Task<ReadOnlyMemory<byte>> OnResponseAsync(UdpConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, UdpProxyDelegate next)
    {
        logger.LogInformation($"udp {DateTime.Now} {context.Features.Get<IL4ReverseProxyFeature>()?.SelectedDestination?.EndPoint.ToString()} reponse size: {source.Length}");
        return next(context, source, token);
    }
}

internal class EchoTcpProxyMiddleware : ITcpProxyMiddleware
{
    private readonly ILogger<EchoTcpProxyMiddleware> logger;

    public EchoTcpProxyMiddleware(ILogger<EchoTcpProxyMiddleware> logger)
    {
        this.logger = logger;
    }

    public Task InitAsync(ConnectionContext context, CancellationToken token, TcpDelegate next)
    {
        return next(context, token);
    }

    public Task<ReadOnlyMemory<byte>> OnRequestAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        logger.LogInformation($"tcp {DateTime.Now} {context.LocalEndPoint.ToString()} request size: {source.Length}");
        return next(context, source, token);
    }

    public Task<ReadOnlyMemory<byte>> OnResponseAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        logger.LogInformation($"tcp {DateTime.Now} {context.Features.Get<IL4ReverseProxyFeature>()?.SelectedDestination?.EndPoint.ToString()} reponse size: {source.Length}");
        return next(context, source, token);
    }
}