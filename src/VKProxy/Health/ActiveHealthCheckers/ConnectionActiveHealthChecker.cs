using Microsoft.AspNetCore.Connections;
using VKProxy.Config;
using VKProxy.Core.Loggers;

namespace VKProxy.Health.ActiveHealthCheckers;

public class ConnectionActiveHealthChecker : ActiveHealthCheckerBase
{
    private readonly IConnectionFactory connectionFactory;

    public override string Name => "Connect";

    public ConnectionActiveHealthChecker(IConnectionFactory connectionFactory, ProxyLogger logger) : base(logger)
    {
        this.connectionFactory = connectionFactory;
    }

    protected override async ValueTask<bool> DoCheckAsync(ActiveHealthCheckConfig config, DestinationState state, CancellationToken cancellationToken)
    {
        var c = await connectionFactory.ConnectAsync(state.EndPoint, cancellationToken);
        c.Abort();
        return true;
    }
}