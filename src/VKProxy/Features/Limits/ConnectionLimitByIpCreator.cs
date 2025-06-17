namespace VKProxy.Features.Limits;

public class ConnectionLimitByIpCreator : IConnectionLimitCreator
{
    public string Name => "Ip";

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options)
    {
        return options.MaxConcurrentConnections.HasValue && options.MaxConcurrentConnections.Value > 0 ? new ConnectionIpLimiter(options.MaxConcurrentConnections.Value, options.HttpIpHeader) : null;
    }
}
