using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VKProxy.Config;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp;

namespace VKProxy;

internal class ListenHandler : ListenHandlerBase
{
    private readonly IConfigSource<IProxyConfig> configSource;
    private readonly ProxyLogger logger;
    private IProxyConfig current;

    public ListenHandler(IConfigSource<IProxyConfig> configSource, ProxyLogger logger)
    {
        this.configSource = configSource;
        this.logger = logger;
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
                }
                catch (Exception ex)
                {
                    logger.BindListenOptionsError(s, ex);
                }
            }
        }
    }

    private async Task OnBindAsync(ITransportManager transportManager, ListenConfig options, CancellationToken cancellationToken)
    {
        if (options.Protocols == GatewayProtocols.UDP)
        {
            await transportManager.BindAsync(options, DoUdp, cancellationToken);
        }
        else if (options.Protocols == GatewayProtocols.TCP)
        {
            await transportManager.BindAsync(options, DoTcp, cancellationToken);
        }
        else
        {
            await transportManager.BindHttpAsync(options, DoHttp, cancellationToken, options.GetHttpProtocols(), true, null, null, options.GetHttpsOptions());
        }
    }

    private async Task DoHttp(HttpContext context)
    {
    }

    private async Task DoTcp(ConnectionContext connection)
    {
    }

    private async Task DoUdp(ConnectionContext connection)
    {
        if (connection is UdpConnectionContext context)
        {
        }
    }

    private Task<(IEnumerable<ListenConfig> stop, IEnumerable<ListenConfig> start)> GenerateDiffAsync(IProxyConfig old, IProxyConfig current, CancellationToken cancellationToken)
    {
        // todo check  option
        return Task.FromResult<(IEnumerable<ListenConfig> stop, IEnumerable<ListenConfig> start)>((null, current.Listen.Values));
    }
}