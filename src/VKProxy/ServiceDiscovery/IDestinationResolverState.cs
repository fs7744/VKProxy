using VKProxy.Config;

namespace VKProxy.ServiceDiscovery;

public interface IDestinationResolverState : IReadOnlyList<DestinationState>, IDisposable
{
}