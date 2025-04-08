using Microsoft.Extensions.Primitives;
using VKProxy.Core.Adapters;

namespace VKProxy.Core.Hosting;

public abstract class ListenHandlerBase : IListenHandler
{
    public abstract Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken);

    public virtual IChangeToken? GetReloadToken()
    {
        return null;
    }

    public virtual Task InitAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task RebindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}