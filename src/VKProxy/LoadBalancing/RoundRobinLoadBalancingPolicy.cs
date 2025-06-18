using System.Runtime.CompilerServices;
using VKProxy.Config;
using VKProxy.Core.Infrastructure;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public sealed class RoundRobinLoadBalancingPolicy : ILoadBalancingPolicy
{
    private readonly ConditionalWeakTable<RouteConfig, AtomicCounter> _counters = new();
    public string Name => LoadBalancingPolicy.RoundRobin;

    public void Init(ClusterConfig cluster)
    {
    }

    public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
    {
        var counter = _counters.GetOrCreateValue(feature.Route);

        // Increment returns the new value and we want the first return value to be 0.
        var offset = counter.Increment() - 1;

        // Preventing negative indices from being computed by masking off sign.
        // Ordering of index selection is consistent across all offsets.
        // There may be a discontinuity when the sign of offset changes.
        return availableDestinations[(offset & 0x7FFFFFFF) % availableDestinations.Count];
    }
}