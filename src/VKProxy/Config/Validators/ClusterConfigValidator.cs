using System.Collections.Frozen;
using System.Net;
using VKProxy.Health;
using VKProxy.LoadBalancing;
using VKProxy.ServiceDiscovery;

namespace VKProxy.Config.Validators;

public class ClusterConfigValidator : IValidator<ClusterConfig>
{
    private readonly IEnumerable<IDestinationResolver> resolvers;
    private readonly FrozenDictionary<string, ILoadBalancingPolicy> policies;
    private readonly IHealthReporter healthReporter;
    private readonly IHealthUpdater healthUpdater;

    public ClusterConfigValidator(IEnumerable<IDestinationResolver> resolvers, IEnumerable<ILoadBalancingPolicy> policies, IHealthReporter healthReporter, IHealthUpdater healthUpdater)
    {
        this.resolvers = resolvers.OrderByDescending(i => i.Order).ToArray();
        this.policies = policies.ToFrozenDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        this.healthReporter = healthReporter;
        this.healthUpdater = healthUpdater;
    }

    public async Task<bool> ValidateAsync(ClusterConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        if (value == null) return false;

        if (policies.TryGetValue(value.LoadBalancingPolicy ?? LoadBalancingPolicy.Random, out var policy))
        {
            value.LoadBalancingPolicyInstance = policy;
        }
        else
        {
            exceptions.Add(new NotSupportedException($"Not supported LoadBalancingPolicy : {value.LoadBalancingPolicy}"));
            return false;
        }

        if (value.HealthCheck != null)
        {
            var passive = value.HealthCheck.Passive;
            if (passive != null)
            {
                passive.ReactivationPeriod = passive.ReactivationPeriod >= passive.DetectionWindowSize ? passive.ReactivationPeriod : passive.DetectionWindowSize;
                value.HealthReporter = healthReporter;
            }
        }

        var destinationStates = new List<DestinationState>();
        List<DestinationConfig> destinationConfigs = new List<DestinationConfig>();
        foreach (var d in value.Destinations)
        {
            var address = d.Address;
            if (IPEndPoint.TryParse(address, out var ip))
            {
                destinationStates.Add(new DestinationState() { EndPoint = ip, ClusterConfig = value });
            }
            else if (address.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(address.AsSpan(10), out var port)
                && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
            {
                destinationStates.Add(new DestinationState() { EndPoint = new IPEndPoint(IPAddress.Loopback, port), ClusterConfig = value });
                destinationStates.Add(new DestinationState() { EndPoint = new IPEndPoint(IPAddress.IPv6Loopback, port), ClusterConfig = value });
            }
            else
            {
                destinationConfigs.Add(d);
            }
        }

        if (destinationConfigs.Count > 0)
        {
            List<IDestinationResolverState> states = new List<IDestinationResolverState>();
            if (resolvers.Any())
            {
                foreach (var resolver in resolvers)
                {
                    try
                    {
                        var r = await resolver.ResolveDestinationsAsync(value, destinationConfigs, cancellationToken);
                        if (r != null)
                        {
                            states.Add(r);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(new InvalidOperationException($"Error resolving destinations for cluster {value.Key}", ex));
                    }
                }
            }
            else
            {
                exceptions.Add(new InvalidOperationException($"No DestinationResolver for cluster {value.Key}"));
            }

            if (destinationStates.Count > 0)
            {
                states.Insert(0, new StaticDestinationResolverState(destinationStates));
            }

            value.DestinationStates = new UnionDestinationResolverState(states);
        }
        else
        {
            if (destinationStates.Count > 0)
            {
                value.DestinationStates = destinationStates;
            }
        }
        healthUpdater.UpdateAvailableDestinations(value);

        return true;
    }
}