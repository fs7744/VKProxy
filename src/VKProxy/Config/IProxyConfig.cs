namespace VKProxy.Config;

public interface IReadOnlyProxyConfig
{
    public IReadOnlyDictionary<string, RouteConfig> Routes { get; }

    public IReadOnlyDictionary<string, ClusterConfig> Clusters { get; }

    public IReadOnlyDictionary<string, ListenConfig> Listen { get; }

    public IReadOnlyDictionary<string, SniConfig> Sni { get; }
}

public interface IProxyConfig : IReadOnlyProxyConfig
{
    public RouteConfig RemoveRoute(string key);

    public ClusterConfig RemoveCluster(string key);

    public ListenConfig RemoveListen(string key);

    public SniConfig RemoveSni(string key);
}