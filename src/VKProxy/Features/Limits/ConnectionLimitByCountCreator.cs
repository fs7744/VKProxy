namespace VKProxy.Features.Limits;

public class ConnectionLimitByCountCreator : IConnectionLimitCreator
{
    public string Name => "Count";

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options)
    {
        return options.MaxConcurrentConnections.HasValue && options.MaxConcurrentConnections.Value > 0 ? new ConnectionLimiter(options.MaxConcurrentConnections.Value) : null;
    }
}