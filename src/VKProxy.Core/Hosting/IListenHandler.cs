using Microsoft.Extensions.Primitives;
using VKProxy.Core.Adapters;

namespace VKProxy.Core.Hosting;

public interface IListenHandler
{
    Task InitAsync(CancellationToken cancellationToken);

    IChangeToken? GetReloadToken();

    Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken);

    Task RebindAsync(ITransportManager transportManager, CancellationToken cancellationToken);
}