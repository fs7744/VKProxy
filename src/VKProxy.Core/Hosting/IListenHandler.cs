using Microsoft.Extensions.Primitives;
using VKProxy.Core.Adapters;

namespace VKProxy.Core.Hosting;

public interface IListenHandler
{
    /// <summary>
    /// init whatever you need
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task InitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// if not need Rebind, please return null
    /// </summary>
    /// <returns></returns>
    IChangeToken? GetReloadToken();

    Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken);

    /// <summary>
    /// Rebind will be called when ReloadToken change
    /// </summary>
    /// <param name="transportManager"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RebindAsync(ITransportManager transportManager, CancellationToken cancellationToken);

    Task StopAsync(ITransportManager transportManager, CancellationToken cancellationToken);
}