﻿using System.Collections.Frozen;
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

    private readonly FrozenDictionary<string, ILoadBalancingPolicy> policies;

    public LoadBalancingPolicy(IEnumerable<ILoadBalancingPolicy> policies)
    {
        this.policies = policies.ToFrozenDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
    }

    public DestinationState? PickDestination(IReverseProxyFeature feature, ClusterConfig clusterConfig = null)
    {
        DestinationState r = null;
        if (feature is not null)
        {
            var route = feature.Route;
            if (clusterConfig == null)
            {
                clusterConfig = route.ClusterConfig;
            }
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
        return r;
    }

    public bool TryGet(string key, out ILoadBalancingPolicy policy)
    {
        return policies.TryGetValue(key, out policy);
    }
}