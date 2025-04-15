namespace VKProxy.Config.Validators;

public class ProxyConfigValidator : IValidator<IProxyConfig>
{
    private readonly IEnumerable<IValidator<ListenConfig>> listenConfigValidators;
    private readonly IEnumerable<IValidator<SniConfig>> sniConfigValidators;
    private readonly IEnumerable<IValidator<RouteConfig>> routeConfigValidators;
    private readonly IEnumerable<IValidator<ClusterConfig>> clusterConfigValidators;

    public ProxyConfigValidator(IEnumerable<IValidator<ListenConfig>> listenConfigValidators,
        IEnumerable<IValidator<SniConfig>> sniConfigValidators,
        IEnumerable<IValidator<RouteConfig>> routeConfigValidators,
        IEnumerable<IValidator<ClusterConfig>> clusterConfigValidators)
    {
        this.listenConfigValidators = listenConfigValidators;
        this.sniConfigValidators = sniConfigValidators;
        this.routeConfigValidators = routeConfigValidators;
        this.clusterConfigValidators = clusterConfigValidators;
    }

    public async Task<bool> ValidateAsync(IProxyConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
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
                    foreach (var v in routeConfigValidators)
                    {
                        if (!(await v.ValidateAsync(ll, exceptions, cancellationToken)))
                        {
                            value.RemoveRoute(l.Key);
                            r = false;
                            break;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(l.Value.ClusterId) && value.Clusters.TryGetValue(l.Value.ClusterId, out var cluster))
                    {
                        l.Value.ClusterConfig = cluster;
                    }
                }
            }

            if (value.Listen != null)
            {
                foreach (var l in value.Listen)
                {
                    var ll = l.Value;
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

            if (value.Sni != null)
            {
                foreach (var l in value.Sni)
                {
                    var ll = l.Value;
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
        }
        return r;
    }
}