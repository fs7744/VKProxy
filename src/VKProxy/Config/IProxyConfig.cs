namespace VKProxy.Config;

public interface IProxyConfig
{
    public IReadOnlyDictionary<string, RouteConfig> Routes { get; }

    public IReadOnlyDictionary<string, ClusterConfig> Clusters { get; }

    public IReadOnlyDictionary<string, ListenConfig> Listen { get; }
}