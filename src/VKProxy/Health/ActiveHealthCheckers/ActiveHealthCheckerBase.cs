using System.Runtime.CompilerServices;
using VKProxy.Config;
using VKProxy.Core.Loggers;

namespace VKProxy.Health.ActiveHealthCheckers;

public abstract class ActiveHealthCheckerBase : IActiveHealthChecker
{
    private readonly ConditionalWeakTable<DestinationState, ActiveHistory> histories = new ConditionalWeakTable<DestinationState, ActiveHistory>();
    private readonly ProxyLogger logger;

    protected ActiveHealthCheckerBase(ProxyLogger logger)
    {
        this.logger = logger;
    }

    public abstract string Name { get; }

    public async Task CheckAsync(ActiveHealthCheckConfig config, DestinationState state, CancellationToken cancellationToken)
    {
        try
        {
            SetStatus(config, state, await DoCheckAsync(config, state, cancellationToken));
        }
        catch (Exception ex)
        {
            SetStatus(config, state, true);
            logger.SocketConnectionCheckFailed(state.EndPoint, ex);
        }
    }

    protected abstract ValueTask<bool> DoCheckAsync(ActiveHealthCheckConfig config, DestinationState state, CancellationToken cancellationToken);

    private void SetStatus(ActiveHealthCheckConfig config, DestinationState state, bool isFailed)
    {
        var h = histories.GetOrCreateValue(state);
        if (isFailed)
        {
            h.Fails++;
            if (h.Fails >= config.Fails)
            {
                state.Health = DestinationHealth.Unhealthy;
                h.Fails = 0;
                h.Passes = 0;
            }
        }
        else
        {
            h.Passes++;
            if (h.Passes >= config.Passes)
            {
                state.Health = DestinationHealth.Healthy;
                h.Fails = 0;
                h.Passes = 0;
            }
        }
    }

    public sealed class ActiveHistory
    {
        public int Passes { get; set; }

        public int Fails { get; set; }
    }
}
