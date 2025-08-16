using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VKProxy.Core.Hosting;

public abstract class BackgroundHostedService : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _runCancellation = new CancellationTokenSource();
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly CancellationTokenRegistration _hostApplicationStoppingRegistration;
    private readonly string _serviceTypeName;
    private bool _disposedValue;
    private Task _runTask;
    protected ILogger Logger { get; set; }

    protected BackgroundHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(hostApplicationLifetime);
        ArgumentNullException.ThrowIfNull(logger);
        _hostApplicationLifetime = hostApplicationLifetime;
        Logger = logger;

        // register the stoppingToken to become cancelled as soon as the
        // shutdown sequence is initiated.
        _hostApplicationStoppingRegistration = _hostApplicationLifetime.ApplicationStopping.Register(_runCancellation.Cancel);

        var serviceType = GetType();
        if (serviceType.IsGenericType)
        {
            _serviceTypeName = $"{serviceType.Name.Split('`').First()}<{string.Join(",", serviceType.GenericTypeArguments.Select(type => type.Name))}>";
        }
        else
        {
            _serviceTypeName = serviceType.Name;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                try
                {
                    _runCancellation.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // ignore redundant exception to allow shutdown sequence to progress uninterrupted
                }

                try
                {
                    _hostApplicationStoppingRegistration.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // ignore redundant exception to allow shutdown sequence to progress uninterrupted
                }
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // fork off a new async causality line beginning with the call to RunAsync
        _runTask = Task.Run(CallRunAsync, CancellationToken.None);

        // the rest of the startup sequence should proceed without delay
        return Task.CompletedTask;

        // entry-point to run async background work separated from the startup sequence
        async Task CallRunAsync()
        {
            // don't bother running in case of abnormally early shutdown
            _runCancellation.Token.ThrowIfCancellationRequested();

            try
            {
                Logger?.LogInformation(
                    new EventId(1, "RunStarting"),
                    "Calling RunAsync for {BackgroundHostedService}",
                    _serviceTypeName);

                try
                {
                    // call the overridden method
                    await RunAsync(_runCancellation.Token).ConfigureAwait(true);
                }
                finally
                {
                    Logger?.LogInformation(
                        new EventId(2, "RunComplete"),
                        "RunAsync completed for {BackgroundHostedService}",
                        _serviceTypeName);
                }
            }
            catch
            {
                if (!_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    // For any exception the application is instructed to tear down.
                    // this would normally happen if IHostedService.StartAsync throws, so it
                    // is a safe assumption the intent of an unhandled exception from background
                    // RunAsync is the same.
                    _hostApplicationLifetime.StopApplication();

                    Logger?.LogInformation(
                        new EventId(3, "RequestedStopApplication"),
                        "Called StopApplication for {BackgroundHostedService}",
                        _serviceTypeName);
                }

                throw;
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // signal for the RunAsync call to be completed
            _runCancellation.Cancel();

            // join the result of the RunAsync causality line back into the results of
            // this StopAsync call. this await statement will not complete until CallRunAsync
            // method has unwound and returned. if RunAsync completed by throwing an exception
            // it will be rethrown by this await. rethrown Exceptions will pass through
            // Hosting and may be caught by Program.Main.
            await _runTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // this exception is ignored - it's a natural result of cancellation token
        }
        finally
        {
            _runTask = null;
        }
    }

    public abstract Task RunAsync(CancellationToken cancellationToken);
}