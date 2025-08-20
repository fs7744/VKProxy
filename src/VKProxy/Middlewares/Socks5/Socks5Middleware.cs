using Microsoft.AspNetCore.Connections;
using System.Collections.Frozen;
using System.Net;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Core.Sockets.Udp.Client;
using VKProxy.Features;
using VKProxy.ServiceDiscovery;

namespace VKProxy.Middlewares.Socks5;

internal class Socks5Middleware : ITcpProxyMiddleware
{
    private readonly IDictionary<byte, ISocks5Auth> auths;
    private readonly IConnectionFactory tcp;
    private readonly IHostResolver hostResolver;
    private readonly ITransportManager transport;
    private readonly IUdpConnectionFactory udp;

    public Socks5Middleware(IEnumerable<ISocks5Auth> socks5Auths, IConnectionFactory tcp, IHostResolver hostResolver, ITransportManager transport, IUdpConnectionFactory udp)
    {
        this.auths = socks5Auths.ToFrozenDictionary(i => i.AuthType);
        this.tcp = tcp;
        this.hostResolver = hostResolver;
        this.transport = transport;
        this.udp = udp;
    }

    public Task InitAsync(ConnectionContext context, CancellationToken token, TcpDelegate next)
    {
        var feature = context.Features.Get<IL4ReverseProxyFeature>();
        if (feature is not null)
        {
            var route = feature.Route;
            if (route is not null && route.Metadata is not null
                && route.Metadata.TryGetValue("socks5", out var b) && bool.TryParse(b, out var isSocks5) && isSocks5)
            {
                feature.IsDone = true;
                return Proxy(context, feature, token);
            }
        }
        return next(context, token);
    }

    public Task<ReadOnlyMemory<byte>> OnRequestAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        return next(context, source, token);
    }

    public Task<ReadOnlyMemory<byte>> OnResponseAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        return next(context, source, token);
    }

    internal async Task Proxy(ConnectionContext context, IL4ReverseProxyFeature feature, CancellationToken token)
    {
        var input = context.Transport.Input;
        var output = context.Transport.Output;
        if (!await Socks5Parser.AuthAsync(input, auths, context, token))
        {
            Abort(context);
            return;
        }
        var cmd = await Socks5Parser.GetCmdRequestAsync(input, token);
        IPEndPoint ip = await ResolveIpAsync(context, cmd, token);
        switch (cmd.Cmd)
        {
            case Socks5Cmd.Connect:
                ConnectionContext upstream;
                try
                {
                    upstream = await tcp.ConnectAsync(ip, token);
                }
                catch
                {
                    await Socks5Parser.ResponeAsync(output, Socks5CmdResponseType.ConnectFail, token);
                    throw;
                }
                await Socks5Parser.ResponeAsync(output, Socks5CmdResponseType.Success, token);
                var task = await Task.WhenAny(
                               context.Transport.Input.CopyToAsync(upstream.Transport.Output, token)
                               , upstream.Transport.Input.CopyToAsync(context.Transport.Output, token));
                if (task.IsCanceled)
                {
                    Abort(upstream);
                    Abort(context);
                    if (task.Exception is not null)
                    {
                        throw task.Exception;
                    }
                }
                break;

            case Socks5Cmd.UdpAssociate:
                var local = context.LocalEndPoint as IPEndPoint;
                var op = new EndPointOptions()
                {
                    EndPoint = new UdpEndPoint(local.Address, 0),
                    Key = Guid.NewGuid().ToString(),
                };
                try
                {
                    var remote = context.RemoteEndPoint;
                    var timeout = feature.Route.Timeout.Value;
                    op.EndPoint = await transport.BindAsync(op, c => ProxyUdp(c as UdpConnectionContext, remote, timeout), token);
                    var c = new CancellationTokenSource();
                    c.CancelAfter(1000);
                    context.ConnectionClosed.Register(state => transport.StopEndpointsAsync(new List<EndPointOptions>() { state as EndPointOptions }, c.Token).ConfigureAwait(false).GetAwaiter().GetResult(), op);
                }
                catch
                {
                    await Socks5Parser.ResponeAsync(output, Socks5CmdResponseType.ConnectFail, token);
                    throw;
                }
                await Socks5Parser.ResponeAsync(output, op.EndPoint as IPEndPoint, Socks5CmdResponseType.Success, token);
                break;
        }
    }

    private static void Abort(ConnectionContext upstream)
    {
        upstream.Transport.Input.CancelPendingRead();
        upstream.Transport.Output.CancelPendingFlush();
        upstream.Abort();
    }

    private async Task ProxyUdp(UdpConnectionContext context, EndPoint remote, TimeSpan timeout)
    {
        using var cts = CancellationTokenSourcePool.Default.Rent(timeout);
        var token = cts.Token;
        if (context.RemoteEndPoint.GetHashCode() == remote.GetHashCode())
        {
            var req = Socks5Parser.GetUdpRequest(context.ReceivedBytes);
            IPEndPoint ip = await ResolveIpAsync(req, token);
            await udp.SendToAsync(context.Socket, ip, req.Data, token);
        }
        else
        {
            await Socks5Parser.UdpResponeAsync(udp, context, remote as IPEndPoint, token);
        }
    }

    private async Task<IPEndPoint> ResolveIpAsync(ConnectionContext context, Socks5Common cmd, CancellationToken token)
    {
        IPEndPoint ip = await ResolveIpAsync(cmd, token);
        if (ip is null)
        {
            await Socks5Parser.ResponeAsync(context.Transport.Output, Socks5CmdResponseType.AddressNotAllow, token);
            Abort(context);
            throw new EntryPointNotFoundException("Address not found");
        }

        return ip;
    }

    private async Task<IPEndPoint> ResolveIpAsync(Socks5Common cmd, CancellationToken token)
    {
        IPEndPoint ip;
        if (cmd.Domain is not null)
        {
            var ips = await hostResolver.HostResolveAsync(cmd.Domain, token);
            if (ips.Length > 0)
            {
                ip = new IPEndPoint(ips.First(), cmd.Port);
            }
            else
                ip = null;
        }
        else if (cmd.Ip is not null)
        {
            ip = new IPEndPoint(cmd.Ip, cmd.Port);
        }
        else
        {
            ip = null;
        }

        return ip;
    }
}