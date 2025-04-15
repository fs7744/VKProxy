using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public class LeastRequestsLoadBalancingPolicy : ILoadBalancingPolicy
{
    public string Name => LoadBalancingPolicy.LeastRequests;

    public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
    {
        var destinationCount = availableDestinations.Count;
        var leastRequestsDestination = availableDestinations[0];
        var leastRequestsCount = leastRequestsDestination.ConcurrentRequestCount;
        for (var i = 1; i < destinationCount; i++)
        {
            var destination = availableDestinations[i];
            var endpointRequestCount = destination.ConcurrentRequestCount;
            if (endpointRequestCount < leastRequestsCount)
            {
                leastRequestsDestination = destination;
                leastRequestsCount = endpointRequestCount;
            }
        }
        return leastRequestsDestination;
    }
}