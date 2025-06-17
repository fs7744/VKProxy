using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using VKProxy.Config;

namespace VKProxy.Features.Limits;

public class ConnectionLimitFactory : IConnectionLimitFactory
{
    private IConnectionLimiter limitConcurrentConnections;

    public ConnectionLimitFactory(IOptions<KestrelServerOptions> options)
    {
        if (options.Value.Limits.MaxConcurrentConnections.HasValue)
            limitConcurrentConnections = Create(new RouteConfig() { MaxConcurrentConnections = options.Value.Limits.MaxConcurrentConnections });
    }

    public IConnectionLimiter? Default => limitConcurrentConnections;

    public IConnectionLimiter? Create(RouteConfig routeConfig)
    {
        if (routeConfig.MaxConcurrentConnections.HasValue)
            return routeConfig.MaxConcurrentConnections.Value > 0 ? new ConnectionLimiter(routeConfig.MaxConcurrentConnections.Value) : null;
        else
            return limitConcurrentConnections;
    }
}