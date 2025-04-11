using VKProxy.Core.Adapters;
using VKProxy.Core.Hosting;

namespace VKProxy;

internal class ListenHandler : ListenHandlerBase
{
    public override Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}