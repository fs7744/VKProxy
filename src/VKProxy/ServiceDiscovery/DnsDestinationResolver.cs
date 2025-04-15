using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Net;
using VKProxy.Config;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Health;

namespace VKProxy.ServiceDiscovery;

public class DnsDestinationResolver : DestinationResolverBase
{
    private readonly ReverseProxyOptions options;
    private readonly ProxyLogger logger;
    private readonly IHealthUpdater healthUpdater;
    private readonly CancellationTokenSourcePool cancellationTokenSourcePool = CancellationTokenSourcePool.Default;

    public DnsDestinationResolver(IOptions<ReverseProxyOptions> options, ProxyLogger logger, IHealthUpdater healthUpdater)
    {
        this.options = options.Value;
        this.logger = logger;
        this.healthUpdater = healthUpdater;
    }

    public override int Order => 0;

    public override async Task ResolveAsync(FuncDestinationResolverState state, CancellationToken cancellationToken)
    {
        List<DestinationState> destinations = new List<DestinationState>();
        foreach (var item in state.Configs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (hostName, port) = AddressParser.Parse(item.Address);
            try
            {
                var addresses = options.DnsAddressFamily switch
                {
                    { } addressFamily => await Dns.GetHostAddressesAsync(hostName, addressFamily, cancellationToken).ConfigureAwait(false),
                    null => await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false)
                };
                destinations.AddRange(addresses.Select(i => new DestinationState() { EndPoint = new IPEndPoint(i, port), ClusterConfig = state.Cluster }));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to resolve host '{hostName}'. See {nameof(Exception.InnerException)} for details.", exception);
            }
        }

        if (options.DnsRefreshPeriod.HasValue && options.DnsRefreshPeriod > TimeSpan.Zero)
        {
            var cts = state.CancellationTokenSource;
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            state.CancellationTokenSource = cts = cancellationTokenSourcePool.Rent();
            cts.CancelAfter(options.DnsRefreshPeriod.Value);
            new CancellationChangeToken(cts.Token).RegisterChangeCallback(o =>
            {
                if (o is FuncDestinationResolverState s)
                {
                    try
                    {
                        ResolveAsync(s, new CancellationTokenSource(options.DnsRefreshPeriod.Value).Token).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                    }
                }
            }, state);
        }
        if (HasChange(state.Destinations, destinations))
        {
            state.Destinations = destinations;
            healthUpdater.UpdateAvailableDestinations(state.Cluster);
        }
    }

    private bool HasChange(IReadOnlyList<DestinationState> destinations1, List<DestinationState> destinations2)
    {
        if (destinations1 is null || destinations1.Count != destinations2.Count) return true;
        foreach (var dest in destinations1)
        {
            if (destinations2.Any(i => i.EndPoint.GetHashCode() == dest.EndPoint.GetHashCode()))
                continue;
            else
                return true;
        }
        foreach (var dest in destinations2)
        {
            if (destinations1.Any(i => i.EndPoint.GetHashCode() == dest.EndPoint.GetHashCode()))
                continue;
            else
                return true;
        }
        return false;
    }
}