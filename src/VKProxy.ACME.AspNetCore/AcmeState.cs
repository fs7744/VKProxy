using Microsoft.Extensions.DependencyInjection;

namespace VKProxy.ACME.AspNetCore;

public abstract class AcmeState : IAcmeState
{
    protected AcmeStateContext context;

    public abstract Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken);

    protected T MoveTo<T>() where T : IAcmeState
    {
        var r = context.ServiceProvider.GetRequiredService<T>();
        if (r is AcmeState state)
        {
            state.context = this.context;
        }
        return r;
    }
}