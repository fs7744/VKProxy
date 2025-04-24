using Microsoft.Extensions.Primitives;
using VKProxy.Core.Adapters;
using VKProxy.Core.Loggers;

namespace VKProxy.Core.Hosting;

public class VKServer : IServer
{
    private readonly ITransportManager transportManager;
    private readonly IHeartbeat heartbeat;
    private readonly IEnumerable<IListenHandler> listenHandlers;
    private readonly GeneralLogger logger;
    private bool _hasStarted;
    private int _stopping;
    private readonly SemaphoreSlim _bindSemaphore = new SemaphoreSlim(initialCount: 1);
    private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
    private readonly TaskCompletionSource _stoppedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public VKServer(ITransportManager transportManager, IHeartbeat heartbeat, IEnumerable<IListenHandler> listenHandlers, GeneralLogger logger)
    {
        this.transportManager = transportManager;
        this.heartbeat = heartbeat;
        this.listenHandlers = listenHandlers;
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
            foreach (var listenHandler in listenHandlers)
            {
                await listenHandler.InitAsync(cancellationToken);
            }
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
            foreach (var listenHandler in listenHandlers)
            {
                IChangeToken? reloadToken = listenHandler.GetReloadToken();
                await listenHandler.BindAsync(transportManager, _stopCts.Token).ConfigureAwait(false);
                reloadToken?.RegisterChangeCallback(TriggerRebind, listenHandler);
            }
        }
        finally
        {
            _bindSemaphore.Release();
        }
    }

    private void TriggerRebind(object? state)
    {
        if (state is IListenHandler listenHandler)
        {
            _ = RebindAsync(listenHandler);
        }
    }

    private async Task RebindAsync(IListenHandler listenHandler)
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
            logger.UnexpectedException("Unable to reload config", ex);
        }
        finally
        {
            reloadToken?.RegisterChangeCallback(TriggerRebind, listenHandler);
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
            foreach (var listenHandler in listenHandlers)
            {
                await listenHandler.StopAsync(transportManager, cancellationToken).ConfigureAwait(false);
            }
            await transportManager.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _stoppedTcs.TrySetException(ex);
            throw;
        }
        finally
        {
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