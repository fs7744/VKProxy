using Microsoft.Extensions.Hosting;

namespace VKProxy.Core.Hosting;

internal class VKHostedService : IHostedService, IAsyncDisposable
{
    private readonly IServer server;

    public VKHostedService(IServer server)
    {
        this.server = server;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(new CancellationToken(canceled: true)).ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return server.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return server.StopAsync(cancellationToken);
    }
}