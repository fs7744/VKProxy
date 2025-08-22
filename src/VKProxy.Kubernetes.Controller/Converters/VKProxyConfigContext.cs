using VKProxy.Config;

namespace VKProxy.Kubernetes.Controller.Converters;

public class VKProxyConfigContext
{
    public Dictionary<string, ClusterConfig> Clusters { get; set; } = new Dictionary<string, ClusterConfig>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, RouteConfig> Routes { get; set; } = new Dictionary<string, RouteConfig>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SniConfig> Sni { get; set; } = new Dictionary<string, SniConfig>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, HashSet<string>> Destinations { get; set; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyProxyConfig Build()
    {
        return new ProxyConfigSnapshot(Routes, Clusters, null, Sni);
    }
}