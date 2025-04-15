using VKProxy.Config;

namespace VKProxy.Health;

public interface IActiveHealthChecker
{
    string Name { get; }

    Task CheckAsync(ActiveHealthCheckConfig config, DestinationState state, CancellationToken cancellationToken);
}