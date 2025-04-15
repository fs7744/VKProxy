using VKProxy.Config;

namespace VKProxy.Health;

public interface IHealthUpdater
{
    void UpdateAvailableDestinations(ClusterConfig cluster);
}