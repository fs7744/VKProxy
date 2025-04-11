using Microsoft.Extensions.Primitives;
using VKProxy.Config;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;

namespace VKProxy;

internal class ListenHandler : ListenHandlerBase
{
    private readonly IConfigSource<IProxyConfig> configSource;
    private IProxyConfig current;

    public ListenHandler(IConfigSource<IProxyConfig> configSource)
    {
        this.configSource = configSource;
    }

    public override async Task InitAsync(CancellationToken cancellationToken)
    {
        current = configSource.CurrentSnapshot;
    }

    public override Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override IChangeToken? GetReloadToken()
    {
        return configSource.GetChangeToken();
    }

    public override Task RebindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        return base.RebindAsync(transportManager, cancellationToken);
    }
}