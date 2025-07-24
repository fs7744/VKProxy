using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VKProxy.ACME.AspNetCore;

public class AcmeLoader : BackgroundService
{
    private readonly IEnumerable<ICertificateSource> sources;
    private readonly ServerCertificateSelector selector;
    private readonly IServiceProvider serviceProvider;
    protected readonly TaskCompletionSource<object?> appStarted = new();

    public AcmeLoader(IHostApplicationLifetime appLifetime, IEnumerable<ICertificateSource> sources, ServerCertificateSelector selector, IServiceProvider serviceProvider)
    {
        this.sources = sources;
        this.selector = selector;
        this.serviceProvider = serviceProvider;
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
        var state = serviceProvider.GetRequiredService<IAcmeState>();
        while (state != null && !stoppingToken.IsCancellationRequested)
        {
            state = await state.MoveNextAsync(stoppingToken);
        }
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