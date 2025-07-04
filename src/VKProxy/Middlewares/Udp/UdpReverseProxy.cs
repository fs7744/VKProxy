﻿using System.Net.Sockets;
using VKProxy.Config;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Core.Sockets.Udp.Client;
using VKProxy.Features;
using VKProxy.LoadBalancing;

namespace VKProxy.Middlewares;

internal class UdpReverseProxy : IUdpReverseProxy
{
    private readonly IUdpConnectionFactory udp;
    private readonly ProxyLogger logger;
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    private readonly UdpProxyDelegate req;
    private readonly UdpProxyDelegate resp;
    private readonly UdpDelegate init;

    public UdpReverseProxy(IUdpConnectionFactory udp, ProxyLogger logger, ILoadBalancingPolicyFactory loadBalancing, IEnumerable<IUdpProxyMiddleware> middlewares)
    {
        this.udp = udp;
        this.logger = logger;
        this.loadBalancing = loadBalancing;
        (init, req, resp) = BuildMiddlewares(middlewares);
    }

    private (UdpDelegate init, UdpProxyDelegate req, UdpProxyDelegate resp) BuildMiddlewares(IEnumerable<IUdpProxyMiddleware> middlewares)
    {
        UdpDelegate init = (context, c) => Task.CompletedTask;
        UdpProxyDelegate req = (context, s, c) => Task.FromResult(s);
        UdpProxyDelegate resp = (context, s, c) => Task.FromResult(s);

        foreach (var middleware in middlewares)
        {
            Func<UdpDelegate, UdpDelegate> m = (next) => (c, t) => middleware.InitAsync(c, t, next);
            init = m(init);

            Func<UdpProxyDelegate, UdpProxyDelegate> r = (next) => (c, s, t) => middleware.OnRequestAsync(c, s, t, next);
            req = r(req);

            r = (next) => (c, s, t) => middleware.OnResponseAsync(c, s, t, next);
            resp = r(resp);
        }

        return (init, req, resp);
    }

    public async Task Proxy(UdpConnectionContext context, IL4ReverseProxyFeature feature)
    {
        var route = feature.Route;
        if (route is null) return;
        logger.ProxyBegin(route.Key);
        try
        {
            using var cts = CancellationTokenSourcePool.Default.Rent(route.Timeout);
            var token = cts.Token;
            await init(context, token);
            if (!feature.IsDone)
            {
                var socket = await DoUdpSendToAsync(null, feature, route, await req(context, context.ReceivedBytes, token), token);
                if (socket != null)
                {
                    var c = route.UdpResponses;
                    while (c > 0)
                    {
                        var r = await udp.ReceiveAsync(socket, token);
                        c--;
                        await udp.SendToAsync(context.Socket, context.RemoteEndPoint, await resp(context, r.GetReceivedBytes(), token), token);
                    }
                    socket.Dispose();
                }
                else
                {
                    logger.NotFoundAvailableUpstream(route.ClusterId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.ConnectUpstreamTimeout(route.Key);
        }
        catch (Exception ex)
        {
            logger.UnexpectedException(nameof(UdpReverseProxy), ex);
        }
        finally
        {
            feature.SelectedDestination?.ConcurrencyCounter.Decrement();
            logger.ProxyEnd(route.Key);
        }
    }

    private async Task<Socket> DoUdpSendToAsync(Socket s, IL4ReverseProxyFeature feature, RouteConfig route, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        Socket socket = s;
        DestinationState selectedDestination = feature.SelectedDestination;
        try
        {
            if (selectedDestination is null)
            {
                feature.SelectedDestination = selectedDestination = loadBalancing.PickDestination(feature);
            }

            if (selectedDestination is null)
            {
                return null;
            }
            var e = selectedDestination.EndPoint;
            if (socket == null || socket.AddressFamily != e.AddressFamily)
            {
                socket?.Dispose();
                socket = new Socket(e.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            }
            await udp.SendToAsync(socket, e, bytes, cancellationToken);
            selectedDestination.ReportSuccessed();
            return socket;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            selectedDestination?.ReportFailed();
            throw;
        }
    }
}