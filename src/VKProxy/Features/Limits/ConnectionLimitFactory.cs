using Microsoft.Extensions.Options;
using System.Collections.Frozen;

namespace VKProxy.Features.Limits;

public class ConnectionLimitFactory : IConnectionLimitFactory
{
    private readonly FrozenDictionary<string, IConnectionLimitCreator> creaters;
    private IConnectionLimiter limitConcurrentConnections;

    public ConnectionLimitFactory(IOptions<ReverseProxyOptions> options, IEnumerable<IConnectionLimitCreator> limitCreators)
    {
        this.creaters = limitCreators.ToFrozenDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        if (options.Value.Limit != null)
            limitConcurrentConnections = Create(options.Value.Limit);
    }

    public IConnectionLimiter? Default => limitConcurrentConnections;

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options)
    {
        if (options != null)
        {
            if (!creaters.TryGetValue(options.Policy ?? "Count", out var connectionLimitCreator))
            {
                connectionLimitCreator = creaters["Count"];
            }
            return connectionLimitCreator.Create(options);
        }
        return Default;
    }
}