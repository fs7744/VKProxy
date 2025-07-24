using Microsoft.Extensions.Hosting;

namespace VKProxy.ACME.AspNetCore;

public class AcmeLoader : BackgroundService
{
    private readonly IEnumerable<ICertificateSource> sources;
    private readonly ServerCertificateSelector selector;
    private readonly IAcmeStateIniter initer;
    protected readonly TaskCompletionSource<object?> appStarted = new();

    public AcmeLoader(IHostApplicationLifetime appLifetime, IEnumerable<ICertificateSource> sources, ServerCertificateSelector selector, IAcmeStateIniter initer)
    {
        this.sources = sources;
        this.selector = selector;
        this.initer = initer;
        appLifetime.ApplicationStarted.Register(() => appStarted.TrySetResult(null));
        if (appLifetime.ApplicationStarted.IsCancellationRequested)
        {
            appStarted.TrySetResult(null);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LoadAll(stoppingToken);
        await appStarted.Task.ConfigureAwait(false);
        await initer.StartAsync(null, stoppingToken);
    }

    private async Task LoadAll(CancellationToken stoppingToken)
    {
        var tasks = sources.Select(i => i.GetCertificatesAsync(stoppingToken));
        await Task.WhenAll(tasks);

        foreach (var item in tasks.SelectMany(i => i.Result).OrderByDescending(i => i.NotAfter))
        {
            selector.Add(item);
        }
    }
}