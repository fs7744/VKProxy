using Microsoft.AspNetCore.Connections;
using System.Collections.Frozen;
using System.Net;
using VKProxy.Features;
using VKProxy.ServiceDiscovery;

namespace VKProxy.Middlewares.Socks5;

internal class Socks5Middleware : ITcpProxyMiddleware
{
    private readonly IDictionary<byte, ISocks5Auth> auths;
    private readonly IConnectionFactory tcp;
    private readonly IHostResolver hostResolver;

    public Socks5Middleware(IEnumerable<ISocks5Auth> socks5Auths, IConnectionFactory tcp, IHostResolver hostResolver)
    {
        this.auths = socks5Auths.ToFrozenDictionary(i => i.AuthType);
        this.tcp = tcp;
        this.hostResolver = hostResolver;
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

    private async Task Proxy(ConnectionContext context, IL4ReverseProxyFeature feature, CancellationToken token)
    {
        var input = context.Transport.Input;
        var output = context.Transport.Output;
        if (!await Socks5Parser.AuthAsync(input, auths, context, token))
        {
            context.Abort();
        }
        var cmd = await Socks5Parser.GetCmdRequestAsync(input, token);
        IPEndPoint ip = await ResolveIpAsync(context, cmd, token);
        switch (cmd.Cmd)
        {
            case Socks5Cmd.Connect:
            case Socks5Cmd.Bind:
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
                    context.Abort();
                }
                break;

            case Socks5Cmd.UdpAssociate:
                //todo udp
                //context.ConnectionClosed.Register(state => {}, udpEndPoint);
                break;
        }
    }

    private async Task<IPEndPoint> ResolveIpAsync(ConnectionContext context, Socks5CmdRequest cmd, CancellationToken token)
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
        if (ip is null)
        {
            await Socks5Parser.ResponeAsync(context.Transport.Output, Socks5CmdResponseType.AddressNotAllow, token);
            context.Abort();
        }

        return ip;
    }
}