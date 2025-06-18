namespace VKProxy.Features.Limits;

public class ConnectionLimitByKeyCreator : IConnectionLimitCreator
{
    public string Name => "Key";

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Header))
            return new ConnectionByKeyLimiter(options, true);
        else if (!string.IsNullOrWhiteSpace(options.Cookie))
            return new ConnectionByKeyLimiter(options, false);
        else
            return null;
    }
}