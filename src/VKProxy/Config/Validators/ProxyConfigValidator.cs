using VKProxy.Middlewares.Http;

namespace VKProxy.Config.Validators;

public class ProxyConfigValidator : IValidator<IProxyConfig>
{
    private readonly IEnumerable<IValidator<ListenConfig>> listenConfigValidators;
    private readonly IEnumerable<IValidator<SniConfig>> sniConfigValidators;
    private readonly IEnumerable<IValidator<RouteConfig>> routeConfigValidators;
    private readonly IEnumerable<IValidator<ClusterConfig>> clusterConfigValidators;
    private readonly IForwarderHttpClientFactory httpClientFactory;

    public ProxyConfigValidator(IEnumerable<IValidator<ListenConfig>> listenConfigValidators,
        IEnumerable<IValidator<SniConfig>> sniConfigValidators,
        IEnumerable<IValidator<RouteConfig>> routeConfigValidators,
        IEnumerable<IValidator<ClusterConfig>> clusterConfigValidators,
        IForwarderHttpClientFactory httpClientFactory)
    {
        this.listenConfigValidators = listenConfigValidators;
        this.sniConfigValidators = sniConfigValidators;
        this.routeConfigValidators = routeConfigValidators;
        this.clusterConfigValidators = clusterConfigValidators;
        this.httpClientFactory = httpClientFactory;
    }

    public async ValueTask<bool> ValidateAsync(IProxyConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        var r = true;

        if (value != null)
        {
            if (value.Clusters != null)
            {
                foreach (var l in value.Clusters)
                {
                    var ll = l.Value;
                    foreach (var v in clusterConfigValidators)
                    {
                        if (!(await v.ValidateAsync(ll, exceptions, cancellationToken)))
                        {
                            value.RemoveCluster(l.Key);
                            r = false;
                            break;
                        }
                    }
                }
            }

            if (value.Routes != null)
            {
                foreach (var l in value.Routes)
                {
                    var ll = l.Value;
                    if (!string.IsNullOrWhiteSpace(l.Value.ClusterId) && value.Clusters.TryGetValue(l.Value.ClusterId, out var cluster))
                    {
                        l.Value.ClusterConfig = cluster;
                    }
                    foreach (var v in routeConfigValidators)
                    {
                        if (!(await v.ValidateAsync(ll, exceptions, cancellationToken)))
                        {
                            value.RemoveRoute(l.Key);
                            r = false;
                            break;
                        }
                    }
                    if (value.Routes.ContainsKey(l.Key))
                    {
                        l.Value.ClusterConfig?.InitHttp(httpClientFactory);
                    }
                }
            }

            if (value.Sni != null)
            {
                foreach (var l in value.Sni)
                {
                    var ll = l.Value;
                    if (!string.IsNullOrWhiteSpace(l.Value.RouteId) && value.Routes.TryGetValue(l.Value.RouteId, out var route))
                    {
                        l.Value.RouteConfig = route;
                    }
                    foreach (var v in sniConfigValidators)
                    {
                        if (!(await v.ValidateAsync(ll, exceptions, cancellationToken)))
                        {
                            value.RemoveListen(l.Key);
                            r = false;
                            break;
                        }
                    }
                }
            }

            if (value.Listen != null)
            {
                foreach (var l in value.Listen)
                {
                    var ll = l.Value;
                    if (!string.IsNullOrWhiteSpace(l.Value.SniId) && value.Sni.TryGetValue(l.Value.SniId, out var sni))
                    {
                        l.Value.SniConfig = sni;
                    }
                    if (!string.IsNullOrWhiteSpace(l.Value.RouteId) && value.Routes.TryGetValue(l.Value.RouteId, out var route))
                    {
                        l.Value.RouteConfig = route;
                    }
                    foreach (var v in listenConfigValidators)
                    {
                        if (!(await v.ValidateAsync(ll, exceptions, cancellationToken)))
                        {
                            value.RemoveListen(l.Key);
                            r = false;
                            break;
                        }
                    }
                    if (ll.ListenEndPointOptions == null || ll.ListenEndPointOptions.Count == 0)
                    {
                        value.RemoveListen(l.Key);
                    }
                }
            }
        }
        return r;
    }
}