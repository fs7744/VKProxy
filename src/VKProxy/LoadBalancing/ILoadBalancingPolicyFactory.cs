using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public interface ILoadBalancingPolicyFactory
{
    DestinationState? PickDestination(IReverseProxyFeature feature, ClusterConfig clusterConfig = null);

    bool TryGet(string key, out ILoadBalancingPolicy policy);
}