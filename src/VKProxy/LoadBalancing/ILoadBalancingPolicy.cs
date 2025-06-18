using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public interface ILoadBalancingPolicy
{
    string Name { get; }

    DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations);

    void Init(ClusterConfig cluster);
}