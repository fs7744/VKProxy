using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public sealed class RandomLoadBalancingPolicy : ILoadBalancingPolicy
{
    public string Name => LoadBalancingPolicy.Random;

    public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
    {
        return availableDestinations[Random.Shared.Next(availableDestinations.Count)];
    }
}