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
    public void RemoveRoute(string key);

    public void RemoveCluster(string key);

    public void RemoveListen(string key);

    public void RemoveSni(string key);
}