using VKProxy.Config;

namespace VKProxy.Kubernetes.Controller.ConfigProvider;

internal class KubernetesEtcdConfigUpdater : IUpdateConfig
{
    private readonly IConfigStorage storage;
    private IProxyConfig old;

    public KubernetesEtcdConfigUpdater(IConfigStorage storage)
    {
        this.storage = storage;
    }

    public async Task UpdateAsync(IProxyConfig config, CancellationToken cancellationToken)
    {
        if (old == null)
        {
            old = await LoadAllAsync(cancellationToken);
        }

        config = ReplaceKeys(config);

        await UpdateClusterDiffAsync(old, config, cancellationToken);
        await UpdateRouteDiffAsync(old, config, cancellationToken);
        await UpdateSniDiffAsync(old, config, cancellationToken);
    }

    private async Task UpdateRouteDiffAsync(IProxyConfig old, IProxyConfig config, CancellationToken cancellationToken)
    {
        var oldRoutes = old.Routes ?? new Dictionary<string, RouteConfig>();
        var newRoutes = config.Routes ?? new Dictionary<string, RouteConfig>();
        foreach (var r in newRoutes)
        {
            await storage.UpdateRouteAsync(r.Value, cancellationToken);
        }

        foreach (var r in oldRoutes)
        {
            if (!newRoutes.ContainsKey(r.Key))
            {
                await storage.DeleteRouteAsync(r.Key, cancellationToken);
            }
        }
    }

    private async Task UpdateClusterDiffAsync(IProxyConfig old, IProxyConfig config, CancellationToken cancellationToken)
    {
        var oldClusters = old.Clusters ?? new Dictionary<string, ClusterConfig>();
        var newClusters = config.Clusters ?? new Dictionary<string, ClusterConfig>();
        foreach (var r in newClusters)
        {
            await storage.UpdateClusterAsync(r.Value, cancellationToken);
        }

        foreach (var r in oldClusters)
        {
            if (!newClusters.ContainsKey(r.Key))
            {
                await storage.DeleteClusterAsync(r.Key, cancellationToken);
            }
        }
    }

    private async Task UpdateSniDiffAsync(IProxyConfig old, IProxyConfig config, CancellationToken cancellationToken)
    {
        var oldSni = old.Sni ?? new Dictionary<string, SniConfig>();
        var newSni = config.Sni ?? new Dictionary<string, SniConfig>();
        foreach (var r in newSni)
        {
            await storage.UpdateSniAsync(r.Value, cancellationToken);
        }

        foreach (var r in oldSni)
        {
            if (!newSni.ContainsKey(r.Key))
            {
                await storage.DeleteSniAsync(r.Key, cancellationToken);
            }
        }
    }

    private IProxyConfig ReplaceKeys(IProxyConfig config)
    {
        return new ProxyConfigSnapshot(config.Routes?.Values.ToDictionary(static v =>
        {
            v.Key = $"/k8s/{v.Key}";
            return v.Key;
        }, StringComparer.OrdinalIgnoreCase),
        config.Clusters?.Values.ToDictionary(static v =>
        {
            v.Key = $"/k8s/{v.Key}";
            return v.Key;
        }, StringComparer.OrdinalIgnoreCase), null,
        config.Sni?.Values.ToDictionary(static v =>
        {
            v.Key = $"/k8s/{v.Key}";
            return v.Key;
        }, StringComparer.OrdinalIgnoreCase));
    }

    private async Task<IProxyConfig?> LoadAllAsync(CancellationToken cancellationToken)
    {
        var routes = (await storage.GetRouteAsync("/k8s/", cancellationToken).ConfigureAwait(false)).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
        var clusters = (await storage.GetClusterAsync("/k8s/", cancellationToken).ConfigureAwait(false)).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
        var sni = (await storage.GetSniAsync("/k8s/", cancellationToken).ConfigureAwait(false)).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
        return new ProxyConfigSnapshot(routes, clusters, null, sni);
    }
}