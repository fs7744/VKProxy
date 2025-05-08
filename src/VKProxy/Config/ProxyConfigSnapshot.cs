using DotNext.Collections.Generic;
using System.Collections.Generic;

namespace VKProxy.Config;

public class ProxyConfigSnapshot : IProxyConfig
{
    private Dictionary<string, RouteConfig> routes;
    private Dictionary<string, ClusterConfig> clusters;
    private Dictionary<string, ListenConfig> listen;
    private Dictionary<string, SniConfig> sni;

    public ProxyConfigSnapshot(Dictionary<string, RouteConfig> routes, Dictionary<string, ClusterConfig> clusters, Dictionary<string, ListenConfig> listen, Dictionary<string, SniConfig> sni)
    {
        this.routes = routes;
        this.clusters = clusters;
        this.listen = listen;
        this.sni = sni;
    }

    public IReadOnlyDictionary<string, RouteConfig> Routes => routes;

    public IReadOnlyDictionary<string, ClusterConfig> Clusters => clusters;

    public IReadOnlyDictionary<string, ListenConfig> Listen => listen;

    public IReadOnlyDictionary<string, SniConfig> Sni => sni;

    public ClusterConfig RemoveCluster(string key)
    {
        if (clusters.TryGetValue(key, out var r))
            clusters.Remove(key);
        return r;
    }

    public ListenConfig RemoveListen(string key)
    {
        if (listen.TryGetValue(key, out var r))
            listen.Remove(key);
        return r;
    }

    public RouteConfig RemoveRoute(string key)
    {
        if (routes.TryGetValue(key, out var r))
            routes.Remove(key);
        return r;
    }

    public SniConfig RemoveSni(string key)
    {
        if (sni.TryGetValue(key, out var r))
            sni.Remove(key);
        return r;
    }

    public void ReplaceListen(string k, ListenConfig v)
    {
        listen[k] = v;
    }

    public void ReplaceRoute(string k, RouteConfig v)
    {
        routes[k] = v;
    }

    public void ReplaceSni(string k, SniConfig? v)
    {
        sni[k] = v;
    }

    public void ReplaceCluster(string k, ClusterConfig? v)
    {
        clusters[k] = v;
    }
}