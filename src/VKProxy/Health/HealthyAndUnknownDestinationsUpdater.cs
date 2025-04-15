using VKProxy.Config;

namespace VKProxy.Health;

public class HealthyAndUnknownDestinationsUpdater : IHealthUpdater
{
    public void UpdateAvailableDestinations(ClusterConfig cluster)
    {
        if (cluster.DestinationStates is null) return;
        if (cluster.HealthCheck != null)
        {
            cluster.AvailableDestinations = cluster.DestinationStates.Where(destination => destination.Health != DestinationHealth.Unhealthy).ToList();
        }
        else
        {
            cluster.AvailableDestinations = cluster.DestinationStates.ToList();
        }
    }
}