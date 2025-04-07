using System.Diagnostics.Contracts;
using System.Threading;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;

namespace VKProxy.Core.Hosting;

public interface IServer
{
    public Task StartAsync(CancellationToken cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken);
}

public interface IListenHandler
{
    void Start();
}

public class VKServer : IServer
{
    private readonly TransportManagerAdapter transportManager;
    private readonly IListenHandler listenHandler;
    private bool _hasStarted;

    public VKServer(TransportManagerAdapter transportManager, IListenHandler listenHandler)
    {
        this.transportManager = transportManager;
        this.listenHandler = listenHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("Server already started");
            }
            _hasStarted = true;
            listenHandler.Start();
            transportManager.StartHeartbeat();
            await BindAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync(new CancellationToken(canceled: true)).GetAwaiter().GetResult();
    }
}