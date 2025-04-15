using VKProxy.Config;

namespace VKProxy.Health;

public interface IActiveHealthCheckMonitor
{
    Task CheckHealthAsync(IEnumerable<ClusterConfig> clusters);
}