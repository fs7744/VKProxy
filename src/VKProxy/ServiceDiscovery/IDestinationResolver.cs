using VKProxy.Config;

namespace VKProxy.ServiceDiscovery;

public interface IDestinationResolver
{
    int Order { get; }

    Task<IDestinationResolverState> ResolveDestinationsAsync(ClusterConfig cluster, List<DestinationConfig> destinationConfigs, CancellationToken cancellationToken);
}