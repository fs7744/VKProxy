using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public sealed class PowerOfTwoChoicesLoadBalancingPolicy : ILoadBalancingPolicy
{
    public string Name => LoadBalancingPolicy.PowerOfTwoChoices;

    public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
    {
        var destinationCount = availableDestinations.Count;

        // Pick two, and then return the least busy. This avoids the effort of searching the whole list, but
        // still avoids overloading a single destination.
        var random = Random.Shared;
        var firstIndex = random.Next(destinationCount);
        int secondIndex;
        do
        {
            secondIndex = random.Next(destinationCount);
        } while (firstIndex == secondIndex);
        var first = availableDestinations[firstIndex];
        var second = availableDestinations[secondIndex];
        return (first.ConcurrentRequestCount <= second.ConcurrentRequestCount) ? first : second;
    }
}