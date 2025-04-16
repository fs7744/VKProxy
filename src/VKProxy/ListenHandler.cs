using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VKProxy.Config;
using VKProxy.Config.Validators;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Features;
using VKProxy.Middlewares;
using VKProxy.Middlewares.Http;

namespace VKProxy;

internal class ListenHandler : ListenHandlerBase
{
    private readonly IConfigSource<IProxyConfig> configSource;
    private readonly ProxyLogger logger;
    private readonly IValidator<IProxyConfig> validator;
    private readonly ISniSelector sniSelector;
    private readonly IUdpReverseProxy udp;
    private readonly ITcpReverseProxy tcp;
    private readonly IHttpReverseProxy http;
    private readonly IHttpSelector httpSelector;
    private IProxyConfig current;

    public ListenHandler(IConfigSource<IProxyConfig> configSource, ProxyLogger logger, IValidator<IProxyConfig> validator,
        ISniSelector sniSelector, IUdpReverseProxy udp, ITcpReverseProxy tcp, IHttpReverseProxy http, IHttpSelector httpSelector)
    {
        this.configSource = configSource;
        this.logger = logger;
        this.validator = validator;
        this.sniSelector = sniSelector;
        this.udp = udp;
        this.tcp = tcp;
        this.http = http;
        this.httpSelector = httpSelector;
    }

    public override Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        current = configSource.CurrentSnapshot;
        return BindAsync(null, current, transportManager, cancellationToken);
    }

    public override IChangeToken? GetReloadToken()
    {
        return configSource.GetChangeToken();
    }

    public override Task RebindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        var old = current;
        current = configSource.CurrentSnapshot;
        return BindAsync(old, current, transportManager, cancellationToken);
    }

    public override Task StopAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        configSource.Dispose();
        return Task.CompletedTask;
    }

    private async Task BindAsync(IProxyConfig old, IProxyConfig current, ITransportManager transportManager, CancellationToken cancellationToken)
    {
        var (stop, start) = await GenerateDiffAsync(old, current, cancellationToken);
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
        var route = options.RouteConfig;
        if (options.Protocols == GatewayProtocols.UDP)
        {
            await transportManager.BindAsync(options, c => DoUdp(c, route), cancellationToken);
        }
        else if (options.Protocols == GatewayProtocols.TCP)
        {
            var sni = options.SniConfig;
            var useSni = options.UseSni;
            await transportManager.BindAsync(options, c => DoTcp(c, route, useSni, sni), cancellationToken);
        }
        else
        {
            var https = options.GetHttpsOptions();
            if (https != null && https.ServerCertificate == null && https.ServerCertificateSelector == null)
            {
                https.ServerCertificateSelector = sniSelector.ServerCertificateSelector;
            }
            await transportManager.BindHttpAsync(options, c => DoHttp(c, route), cancellationToken, options.GetHttpProtocols(), true, null, null, https);
        }
    }

    private Task DoHttp(HttpContext context, RouteConfig? route)
    {
        var proxyFeature = new ReverseProxyFeature() { Route = route };
        context.Features.Set<IReverseProxyFeature>(proxyFeature);
        return http.Proxy(context, proxyFeature);
    }

    private Task DoTcp(ConnectionContext connection, RouteConfig? route, bool useSni, SniConfig? sni)
    {
        var proxyFeature = new ReverseProxyFeature() { Route = route, IsSni = useSni, SelectedSni = sni };
        connection.Features.Set<IReverseProxyFeature>(proxyFeature);
        return tcp.Proxy(connection, proxyFeature);
    }

    private Task DoUdp(ConnectionContext connection, RouteConfig? route)
    {
        if (connection is UdpConnectionContext context)
        {
            var proxyFeature = new ReverseProxyFeature() { Route = route };
            context.Features.Set<IReverseProxyFeature>(proxyFeature);
            return udp.Proxy(context, proxyFeature);
        }
        return Task.CompletedTask;
    }

    private async Task<(IEnumerable<ListenEndPointOptions> stop, IEnumerable<ListenEndPointOptions> start)> GenerateDiffAsync(IProxyConfig old, IProxyConfig current, CancellationToken cancellationToken)
    {
        if (current == null && old == null) return (null, null);

        var errors = new List<Exception>();
        if (!await validator.ValidateAsync(current, errors, cancellationToken))
        {
            foreach (var error in errors)
            {
                logger.ErrorConfig(error.Message);
            }
        }
        await sniSelector.ReBuildAsync(current.Sni, cancellationToken);
        await httpSelector.ReBuildAsync(current.Routes, cancellationToken);
        //todo diff
        return (null, current?.Listen.Values.SelectMany(i => i.ListenEndPointOptions));
    }
}