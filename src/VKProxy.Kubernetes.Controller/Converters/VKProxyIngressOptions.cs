using VKProxy.Config;
using VKProxy.Features.Limits;

namespace VKProxy.Kubernetes.Controller.Converters;

public class VKProxyIngressOptions
{
    public bool Https { get; set; }
    public List<Dictionary<string, string>> Transforms { get; set; }
    public string LoadBalancingPolicy { get; set; }
    public HealthCheckConfig HealthCheck { get; set; }
    public int RouteOrder { get; set; }
    public ForwarderRequestConfig HttpRequest { get; set; }
    public HttpClientConfig HttpClientConfig { get; set; }
    public Dictionary<string, string> ClusterMetadata { get; set; }
    public HashSet<string> RouteMethods { get; set; }
    public string RouteStatement { get; set; }
    public Dictionary<string, string> RouteMetadata { get; set; }
    public TimeSpan? Timeout { get; set; }
    public ConcurrentConnectionLimitOptions Limit { get; set; }
}