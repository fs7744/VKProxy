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

    public void RemoveCluster(string key)
    {
        clusters.Remove(key);
    }

    public void RemoveListen(string key)
    {
        listen.Remove(key);
    }

    public void RemoveRoute(string key)
    {
        routes.Remove(key);
    }

    public void RemoveSni(string key)
    {
        sni.Remove(key);
    }
}