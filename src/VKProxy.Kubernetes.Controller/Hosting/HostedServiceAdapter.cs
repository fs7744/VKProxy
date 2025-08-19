using Microsoft.Extensions.Hosting;

namespace VKProxy.Kubernetes.Controller.Hosting;

public class HostedServiceAdapter<TService> : IHostedService
    where TService : IHostedService
{
    private readonly TService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedServiceAdapter{TInterface}" /> class.
    /// </summary>
    /// <param name="service">The service interface to delegate onto.</param>
    public HostedServiceAdapter(TService service) => _service = service;

    public Task StartAsync(CancellationToken cancellationToken) => _service.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => _service.StopAsync(cancellationToken);
}