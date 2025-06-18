using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public sealed class LoadBalancingPolicy : ILoadBalancingPolicyFactory
{
    public static string Random => nameof(Random);
    public static string RoundRobin => nameof(RoundRobin);
    public static string LeastRequests => nameof(LeastRequests);
    public static string PowerOfTwoChoices => nameof(PowerOfTwoChoices);
    public static string Hash => nameof(Hash);

    public DestinationState? PickDestination(IReverseProxyFeature feature)
    {
        DestinationState r = null;
        if (feature is not null)
        {
            var route = feature.Route;
            var clusterConfig = route.ClusterConfig;
            if (!(clusterConfig is null || clusterConfig.AvailableDestinations is null))
            {
                var states = clusterConfig.AvailableDestinations;
                if (!(states is null || states.Count == 0))
                {
                    if (states.Count == 1)
                    {
                        r = states[0];
                    }
                    else
                    {
                        r = clusterConfig.LoadBalancingPolicyInstance.PickDestination(feature, states);
                    }
                }
            }
        }
        feature.SelectedDestination = r;
        return r;
    }
}