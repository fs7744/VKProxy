using System.Collections.Frozen;
using System.Diagnostics.Metrics;
using VKProxy.Health;
using VKProxy.LoadBalancing;
using VKProxy.LoadBalancing.SessionAffinity;
using VKProxy.Middlewares.Http;
using VKProxy.ServiceDiscovery;

namespace VKProxy.Config.Validators;

public class ClusterConfigValidator : IValidator<ClusterConfig>
{
    private readonly IEnumerable<IDestinationResolver> resolvers;
    private readonly ILoadBalancingPolicyFactory policies;
    private readonly IHealthReporter healthReporter;
    private readonly IHealthUpdater healthUpdater;
    private readonly IEnumerable<IDestinationConfigParser> destinationConfigParsers;
    private readonly IForwarderHttpClientFactory httpClientFactory;
    private readonly SessionAffinityLoadBalancingPolicy sessionAffinity;

    public ClusterConfigValidator(IEnumerable<IDestinationResolver> resolvers, ILoadBalancingPolicyFactory policies, IHealthReporter healthReporter,
        IHealthUpdater healthUpdater, IEnumerable<IDestinationConfigParser> destinationConfigParsers, IForwarderHttpClientFactory httpClientFactory,
        SessionAffinityLoadBalancingPolicy sessionAffinity)
    {
        this.resolvers = resolvers.OrderBy(i => i.Order).ToArray();
        this.policies = policies;
        this.healthReporter = healthReporter;
        this.healthUpdater = healthUpdater;
        this.destinationConfigParsers = destinationConfigParsers;
        this.httpClientFactory = httpClientFactory;
        this.sessionAffinity = sessionAffinity;
    }

    public async ValueTask<bool> ValidateAsync(ClusterConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        if (value == null) return false;
        if (value.DestinationStates != null) return true;

        if (policies.TryGet(value.LoadBalancingPolicy ?? LoadBalancingPolicy.Random, out var policy))
        {
            value.LoadBalancingPolicyInstance = policy;
            policy?.Init(value);
            sessionAffinity.Init(value);
            if (value.Metadata != null && value.Metadata.TryGetValue("SessionAffinity", out var way) && !string.IsNullOrWhiteSpace(way))
            {
            }
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
            if (value.HealthCheck.Active != null && string.Equals(value.HealthCheck.Active.Policy, "http", StringComparison.OrdinalIgnoreCase))
            {
                value.InitHttp(httpClientFactory);
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
                    state.ClusterConfig = value;
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
                            break;
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