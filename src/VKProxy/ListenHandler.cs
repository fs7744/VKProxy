using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VKProxy.Config;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Features;
using VKProxy.Middlewares;

namespace VKProxy;

internal class ListenHandler : ListenHandlerBase
{
    private readonly IConfigSource<IProxyConfig> configSource;
    private readonly ProxyLogger logger;
    private readonly IHttpSelector httpSelector;
    private readonly ISniSelector sniSelector;
    private readonly IUdpReverseProxy udp;
    private readonly ITcpReverseProxy tcp;
    private readonly RequestDelegate http;

    public ListenHandler(IConfigSource<IProxyConfig> configSource, ProxyLogger logger, IHttpSelector httpSelector,
        ISniSelector sniSelector, IUdpReverseProxy udp, ITcpReverseProxy tcp, IApplicationBuilder applicationBuilder)
    {
        this.configSource = configSource;
        this.logger = logger;
        this.httpSelector = httpSelector;
        this.sniSelector = sniSelector;
        this.udp = udp;
        this.tcp = tcp;
        this.http = applicationBuilder.Build();
    }

    public override Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        return BindDiffAsync(transportManager, cancellationToken);
    }

    public override IChangeToken? GetReloadToken()
    {
        return configSource.GetChangeToken();
    }

    public override Task RebindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        return BindDiffAsync(transportManager, cancellationToken);
    }

    public override Task StopAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        configSource.Dispose();
        return Task.CompletedTask;
    }

    private async Task BindDiffAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        var (stop, start) = await configSource.GenerateDiffAsync(cancellationToken);
        if (stop != null)
        {
            await transportManager.StopEndpointsAsync(stop.ToList<EndPointOptions>(), cancellationToken);
        }

        if (start != null)
        {
            foreach (var s in start)
            {
                try
                {
                    await OnBindAsync(transportManager, s, cancellationToken).ConfigureAwait(false);
                    logger.BindListenOptions(s);
                }
                catch (Exception ex)
                {
                    logger.BindListenOptionsError(s, ex);
                }
            }
        }
    }

    private async Task OnBindAsync(ITransportManager transportManager, ListenEndPointOptions options, CancellationToken cancellationToken)
    {
        if (options.Protocols == GatewayProtocols.UDP)
        {
            await transportManager.BindAsync(options, c => DoUdp(c, options), cancellationToken);
        }
        else if (options.Protocols == GatewayProtocols.TCP)
        {
            await transportManager.BindAsync(options, c => DoTcp(c, options), cancellationToken);
        }
        else
        {
            var https = options.GetHttpsOptions();
            if (https != null && https.ServerCertificate == null && https.ServerCertificateSelector == null)
            {
                https.ServerCertificateSelector = sniSelector.ServerCertificateSelector;
            }
            await transportManager.BindHttpAsync(options, c => DoHttp(c, options), cancellationToken, options.GetHttpProtocols(), true, null, null, https);
        }
    }

    private async Task DoHttp(HttpContext context, ListenEndPointOptions? options)
    {
        var proxyFeature = new L7ReverseProxyFeature() { Route = options?.RouteConfig ?? await httpSelector.MatchAsync(context) };
        context.Features.Set<IReverseProxyFeature>(proxyFeature);
        await http(context);
    }

    private Task DoTcp(ConnectionContext connection, ListenEndPointOptions? options)
    {
        var proxyFeature = new L4ReverseProxyFeature() { Route = options?.RouteConfig, IsSni = options?.UseSni ?? false, SelectedSni = options?.SniConfig };
        connection.Features.Set<IL4ReverseProxyFeature>(proxyFeature);
        return tcp.Proxy(connection, proxyFeature);
    }

    private Task DoUdp(ConnectionContext connection, ListenEndPointOptions? options)
    {
        if (connection is UdpConnectionContext context)
        {
            var proxyFeature = new L4ReverseProxyFeature() { Route = options?.RouteConfig };
            context.Features.Set<IL4ReverseProxyFeature>(proxyFeature);
            return udp.Proxy(context, proxyFeature);
        }
        return Task.CompletedTask;
    }
}