using VKProxy.Config;

namespace VKProxy.Kubernetes.Controller.Converters;

internal class VKProxyConfigContext
{
    public Dictionary<string, ClusterConfig> Clusters { get; set; } = new Dictionary<string, ClusterConfig>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, RouteConfig> Routes { get; set; } = new Dictionary<string, RouteConfig>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SniConfig> Snis { get; set; } = new Dictionary<string, SniConfig>(StringComparer.OrdinalIgnoreCase);
}