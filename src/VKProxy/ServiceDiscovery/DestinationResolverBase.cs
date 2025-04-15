using VKProxy.Config;

namespace VKProxy.ServiceDiscovery;

public abstract class DestinationResolverBase : IDestinationResolver
{
    public abstract int Order { get; }

    public async Task<IDestinationResolverState> ResolveDestinationsAsync(ClusterConfig cluster, List<DestinationConfig> destinationConfigs, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var r = new FuncDestinationResolverState(cluster, destinationConfigs, ResolveAsync);
        await r.ResolveAsync(cancellationToken);
        return r;
    }

    public abstract Task ResolveAsync(FuncDestinationResolverState state, CancellationToken cancellationToken);
}