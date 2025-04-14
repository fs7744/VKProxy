using VKProxy.Core.Infrastructure;
using VKProxy.Health;
using VKProxy.LoadBalancing;

namespace VKProxy.Config;

public class ClusterConfig
{
    public string Key { get; set; }

    public string? LoadBalancingPolicy { get; init; }

    public HealthCheckConfig? HealthCheck { get; init; }

    public IReadOnlyList<DestinationConfig>? Destinations { get; init; }

    internal IReadOnlyList<DestinationState> DestinationStates { get; set; }

    internal ILoadBalancingPolicy LoadBalancingPolicyInstance { get; set; }
    internal List<DestinationState> AvailableDestinations { get; set; }
    internal IHealthReporter HealthReporter { get; set; }

    public void Dispose()
    {
        DestinationStates = null;
        LoadBalancingPolicyInstance = null;
    }

    public bool Equals(ClusterConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase)
            && string.Equals(LoadBalancingPolicy, other.LoadBalancingPolicy, StringComparison.OrdinalIgnoreCase)
            && HealthCheckConfig.Equals(HealthCheck, other.HealthCheck)
            && CollectionUtilities.Equals(Destinations, other.Destinations, DestinationConfig.Comparer);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Key?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            LoadBalancingPolicy?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            HealthCheck,
            CollectionUtilities.GetHashCode(Destinations));
    }
}