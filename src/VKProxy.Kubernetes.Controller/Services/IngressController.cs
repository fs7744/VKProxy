using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VKProxy.Core.Hosting;
using VKProxy.Kubernetes.Controller.Caching;
using VKProxy.Kubernetes.Controller.Client;
using VKProxy.Kubernetes.Controller.Queues;

namespace VKProxy.Kubernetes.Controller.Services;

public interface IK8SChange
{
    WatchEventType WatchEventType { get; }
}

public class K8SChange<T> : IK8SChange
{
    public WatchEventType WatchEventType { get; set; }
    public T Data { get; set; }
}

public class IngressController : BackgroundHostedService
{
    private readonly IReadOnlyList<IResourceInformerRegistration> _registrations;
    private readonly ICache _cache;
    private readonly IReconciler _reconciler;

    private bool _registrationsReady;
    private readonly WorkQueue<QueueItem> _queue;
    private readonly QueueItem _ingressChangeQueueItem;

    public IngressController(
        ICache cache,
        IReconciler reconciler,
        IServiceProvider provider,
        IResourceInformer<V1Ingress> ingressInformer,
        IResourceInformer<V1Service> serviceInformer,
        IResourceInformer<V1IngressClass> ingressClassInformer,
        IResourceInformer<V1Secret> secretInformer,
        IOptions<K8sOptions> options,
        IHostApplicationLifetime hostApplicationLifetime, ILogger<IngressController> logger) : base(hostApplicationLifetime, logger)
    {
        ArgumentNullException.ThrowIfNull(ingressInformer, nameof(ingressInformer));
        ArgumentNullException.ThrowIfNull(serviceInformer, nameof(serviceInformer));
        ArgumentNullException.ThrowIfNull(ingressClassInformer, nameof(ingressClassInformer));
        ArgumentNullException.ThrowIfNull(secretInformer, nameof(secretInformer));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        var watchSecrets = options.Value.ServerCertificates;

        var registrations = new List<IResourceInformerRegistration>()
        {
            serviceInformer.Register(Notification),
            ingressClassInformer.Register(Notification),
            ingressInformer.Register(Notification),
        };
        var oldEnd = options.Value.K8SVersion >= 1.33 ? null : provider.GetRequiredService<IResourceInformer<V1Endpoints>>();
        if (oldEnd != null)
        {
            registrations.Add(oldEnd.Register(Notification));
        }
        var newEnd = options.Value.K8SVersion >= 1.33 ? provider.GetRequiredService<IResourceInformer<V1EndpointSlice>>() : null;
        if (newEnd != null)
        {
            registrations.Add(newEnd.Register(Notification));
        }
        if (watchSecrets)
        {
            registrations.Add(secretInformer.Register(Notification));
        }

        _registrations = registrations;
        _registrationsReady = false;
        serviceInformer.StartWatching();
        oldEnd?.StartWatching();
        newEnd?.StartWatching();
        ingressClassInformer.StartWatching();
        ingressInformer.StartWatching();
        if (watchSecrets)
        {
            secretInformer.StartWatching();
        }

        _queue = new ProcessingRateLimitedQueue<QueueItem>(perSecond: 0.5, burst: 1);

        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(reconciler);

        _cache = cache;
        _reconciler = reconciler;

        _ingressChangeQueueItem = new QueueItem("Ingress Change");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var registration in _registrations)
            {
                registration.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1Ingress resource)
    {
        if (_cache.Update(eventType, resource))
        {
            NotificationIngressChanged();
        }
    }

    private void NotificationIngressChanged()
    {
        if (!_registrationsReady)
        {
            return;
        }

        _queue.Add(_ingressChangeQueueItem);
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1Service resource)
    {
        var ingressNames = _cache.Update(eventType, resource);
        if (ingressNames.Count > 0)
        {
            NotificationIngressChanged();
        }
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1Endpoints resource)
    {
        var ingressNames = _cache.Update(eventType, resource);
        if (ingressNames.Count > 0)
        {
            NotificationIngressChanged();
        }
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1EndpointSlice resource)
    {
        var ingressNames = _cache.Update(eventType, resource);
        if (ingressNames.Count > 0)
        {
            NotificationIngressChanged();
        }
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1IngressClass resource)
    {
        _cache.Update(eventType, resource);
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1Secret resource)
    {
        _cache.Update(eventType, resource);
    }

    public override async Task RunAsync(CancellationToken cancellationToken)
    {
        // First wait for all informers to fully List resources before processing begins.
        foreach (var registration in _registrations)
        {
            await registration.ReadyAsync(cancellationToken).ConfigureAwait(false);
        }

        // At this point we know that all the Ingress and Endpoint caches are at least in sync
        // with cluster's state as of the start of this controller.
        _registrationsReady = true;
        NotificationIngressChanged();
        // Now begin one loop to process work until an application shutdown is requested.
        while (!cancellationToken.IsCancellationRequested)
        {
            // Dequeue the next item to process
            var (item, shutdown) = await _queue.GetAsync(cancellationToken).ConfigureAwait(false);
            if (shutdown)
            {
                Logger.LogInformation("Work queue has been shutdown. Exiting reconciliation loop.");
                return;
            }

            try
            {
                await _reconciler.ProcessAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Logger.LogInformation("Rescheduling {Change}", item.Change);

                // Any failure to process this item results in being re-queued
                _queue.Add(item);
            }
            finally
            {
                _queue.Done(item);
            }
        }

        Logger.LogInformation("Reconciliation loop cancelled");
    }
}