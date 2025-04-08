using Microsoft.Extensions.Primitives;
using VKProxy.Core.Adapters;
using VKProxy.Core.Loggers;

namespace VKProxy.Core.Hosting;

public class VKServer : IServer
{
    private readonly ITransportManager transportManager;
    private readonly IHeartbeat heartbeat;
    private readonly IListenHandler listenHandler;
    private readonly GeneralLogger logger;
    private bool _hasStarted;
    private int _stopping;
    private readonly SemaphoreSlim _bindSemaphore = new SemaphoreSlim(initialCount: 1);
    private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
    private readonly TaskCompletionSource _stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private IDisposable? _configChangedRegistration;

    public VKServer(ITransportManager transportManager, IHeartbeat heartbeat, IListenHandler listenHandler, GeneralLogger logger)
    {
        this.transportManager = transportManager;
        this.heartbeat = heartbeat;
        this.listenHandler = listenHandler;
        this.logger = logger;
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
            await listenHandler.InitAsync(cancellationToken);
            heartbeat.StartHeartbeat();
            await BindAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private async Task BindAsync(CancellationToken cancellationToken)
    {
        await _bindSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_stopping == 1)
            {
                throw new InvalidOperationException("Server has already been stopped.");
            }

            IChangeToken? reloadToken = listenHandler.GetReloadToken();
            await listenHandler.BindAsync(transportManager, _stopCts.Token).ConfigureAwait(false);
            _configChangedRegistration = reloadToken?.RegisterChangeCallback(TriggerRebind, this);
        }
        finally
        {
            _bindSemaphore.Release();
        }
    }

    private void TriggerRebind(object? state)
    {
        if (state is VKServer server)
        {
            _ = server.RebindAsync();
        }
    }

    private async Task RebindAsync()
    {
        await _bindSemaphore.WaitAsync();

        IChangeToken? reloadToken = null;
        try
        {
            if (_stopping == 1)
            {
                return;
            }

            reloadToken = listenHandler.GetReloadToken();
            await listenHandler.RebindAsync(transportManager, _stopCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.UnexpectedException("Unable to reload configuration", ex);
        }
        finally
        {
            _configChangedRegistration = reloadToken?.RegisterChangeCallback(TriggerRebind, this);
            _bindSemaphore.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            await _stoppedTcs.Task.ConfigureAwait(false);
            return;
        }

        heartbeat.StopHeartbeat();

        _stopCts.Cancel();

        await _bindSemaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            await transportManager.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _stoppedTcs.TrySetException(ex);
            throw;
        }
        finally
        {
            _configChangedRegistration?.Dispose();
            _stopCts.Dispose();
            _bindSemaphore.Release();
        }

        _stoppedTcs.TrySetResult();
    }

    public void Dispose()
    {
        StopAsync(new CancellationToken(canceled: true)).GetAwaiter().GetResult();
    }
}