using System.Collections.Frozen;
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
    private readonly IEnumerable<IDestinationConfigParser> destinationConfigParsers;

    public ClusterConfigValidator(IEnumerable<IDestinationResolver> resolvers, IEnumerable<ILoadBalancingPolicy> policies, IHealthReporter healthReporter,
        IHealthUpdater healthUpdater, IEnumerable<IDestinationConfigParser> destinationConfigParsers)
    {
        this.resolvers = resolvers.OrderByDescending(i => i.Order).ToArray();
        this.policies = policies.ToFrozenDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        this.healthReporter = healthReporter;
        this.healthUpdater = healthUpdater;
        this.destinationConfigParsers = destinationConfigParsers;
    }

    public async ValueTask<bool> ValidateAsync(ClusterConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        if (value == null) return false;
        if (value.DestinationStates != null) return true;

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

        List<IDestinationResolverState> states = new List<IDestinationResolverState>();

        var destinationStates = new List<DestinationState>();
        List<DestinationConfig> destinationConfigs = new List<DestinationConfig>();
        foreach (var d in value.Destinations.Where(i => !string.IsNullOrWhiteSpace(i.Address)))
        {
            var handled = false;
            foreach (var parser in this.destinationConfigParsers)
            {
                if (parser.TryParse(d, out var state))
                {
                    handled = true;
                    destinationStates.Add(state);
                    break;
                }
            }

            if (!handled)
            {
                destinationConfigs.Add(d);
            }
        }

        if (destinationStates.Count > 0)
        {
            states.Add(new StaticDestinationResolverState(destinationStates));
        }

        if (destinationConfigs.Count > 0)
        {
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
        }

        if (states.Count == 1)
        {
            value.DestinationStates = states[0];
        }
        else
        {
            value.DestinationStates = new UnionDestinationResolverState(states);
        }
        healthUpdater.UpdateAvailableDestinations(value);

        return true;
    }
}