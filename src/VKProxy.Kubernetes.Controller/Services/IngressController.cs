using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VKProxy.Core.Hosting;
using VKProxy.Kubernetes.Controller.Caching;
using VKProxy.Kubernetes.Controller.Client;

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
    private readonly IReconciler _reconciler;
    private readonly Lock _lock = new Lock();
    private Queue<IK8SChange> _pendingChanges;
    private readonly Dictionary<string, IngressClassData> _ingressClassData = new Dictionary<string, IngressClassData>();
    private readonly K8sOptions _options;
    private bool _isDefaultController;
    private readonly Lock _sync = new();

    public IngressController(
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
        _options = options.Value;
        var watchSecrets = options.Value.ServerCertificates;

        var registrations = new List<IResourceInformerRegistration>()
        {
            serviceInformer.Register(Notification),
            ingressClassInformer.Register(Notification),
            ingressInformer.Register(Notification),
        };
        var oldEnd = provider.GetService<IResourceInformer<V1Endpoints>>();
        if (oldEnd != null)
        {
            registrations.Add(oldEnd.Register(Notification));
        }
        var newEnd = provider.GetService<IResourceInformer<V1EndpointSlice>>();
        if (newEnd != null)
        {
            registrations.Add(newEnd.Register(Notification));
        }
        if (watchSecrets)
        {
            registrations.Add(secretInformer.Register(Notification));
        }

        _registrations = registrations;
        ChangePenndingQueue();
        serviceInformer.StartWatching();
        oldEnd?.StartWatching();
        newEnd?.StartWatching();
        ingressClassInformer.StartWatching();
        ingressInformer.StartWatching();
        if (watchSecrets)
        {
            secretInformer.StartWatching();
        }

        this._reconciler = reconciler;
    }

    private Queue<IK8SChange> ChangePenndingQueue()
    {
        lock (_lock)
        {
            var r = _pendingChanges;
            _pendingChanges = new Queue<IK8SChange>();
            return r;
        }
    }

    private void AddPennding(IK8SChange change)
    {
        lock (_lock)
        {
            _pendingChanges.Enqueue(change);
        }
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
        AddPennding(new K8SChange<V1Ingress>() { Data = resource, WatchEventType = eventType });
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1Service resource)
    {
        AddPennding(new K8SChange<V1Service>() { Data = resource, WatchEventType = eventType });
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1Endpoints resource)
    {
        AddPennding(new K8SChange<V1Endpoints>() { Data = resource, WatchEventType = eventType });
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1EndpointSlice resource)
    {
        AddPennding(new K8SChange<V1EndpointSlice>() { Data = resource, WatchEventType = eventType });
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1IngressClass resource)
    {
        AddPennding(new K8SChange<V1IngressClass>() { Data = resource, WatchEventType = eventType });
        if (!string.Equals(_options.ControllerClass, resource.Spec.Controller, StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogInformation(
                "Ignoring {IngressClassNamespace}/{IngressClassName} as the spec.controller is not the same as this ingress",
                resource.Metadata.NamespaceProperty,
                resource.Metadata.Name);
            return;
        }

        var ingressClassName = resource.Name();
        lock (_sync)
        {
            if (eventType == WatchEventType.Added || eventType == WatchEventType.Modified)
            {
                _ingressClassData[ingressClassName] = new IngressClassData(resource);
            }
            else if (eventType == WatchEventType.Deleted)
            {
                _ingressClassData.Remove(ingressClassName);
            }

            _isDefaultController = _ingressClassData.Values.Any(ic => ic.IsDefault);
        }
    }

    /// <summary>
    /// Called by the informer with real-time resource updates.
    /// </summary>
    /// <param name="eventType">Indicates if the resource new, updated, or deleted.</param>
    /// <param name="resource">The information as provided by the Kubernetes API server.</param>
    private void Notification(WatchEventType eventType, V1Secret resource)
    {
        AddPennding(new K8SChange<V1Secret>() { Data = resource, WatchEventType = eventType });
    }

    public override async Task RunAsync(CancellationToken cancellationToken)
    {
        // First wait for all informers to fully List resources before processing begins.
        foreach (var registration in _registrations)
        {
            await registration.ReadyAsync(cancellationToken).ConfigureAwait(false);
        }

        // Now begin one loop to process work until an application shutdown is requested.
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(500).ConfigureAwait(false);
            var queue = ChangePenndingQueue();
            if (queue is null || queue.Count == 0)
                continue;
            try
            {
                await _reconciler.ProcessAsync(Reduce(queue), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
            }
        }

        Logger.LogInformation("Reconciliation loop cancelled");
    }

    private IEnumerable<IK8SChange> Reduce(Queue<IK8SChange> queue)
    {
        var dict = new Dictionary<string, IK8SChange>();
        while (queue.TryDequeue(out var r))
        {
            if (r is K8SChange<V1Ingress> ing)
            {
                if (IsVKProxyIngress(ing.Data))
                {
                    var key = $"V1Ingress/{ing.Data.Namespace()}/{ing.Data.Name()}";
                    dict[key] = r;
                }
            }
            else if (r is K8SChange<V1Service> svc)
            {
                var key = $"V1Service/{svc.Data.Namespace()}/{svc.Data.Name()}";
                dict[key] = r;
            }
            else if (r is K8SChange<V1Endpoints> end)
            {
                var key = $"V1Endpoints/{end.Data.Namespace()}/{end.Data.Name()}";
                dict[key] = r;
            }
            else if (r is K8SChange<V1EndpointSlice> endp)
            {
                var key = $"V1EndpointSlice/{endp.Data.Namespace()}/{endp.Data.Name()}";
                dict[key] = r;
            }
            else if (r is K8SChange<V1IngressClass> ic)
            {
                var key = $"V1IngressClass/{ic.Data.Namespace()}/{ic.Data.Name()}";
                dict[key] = r;
            }
            else if (r is K8SChange<V1Secret> sec)
            {
                var key = $"V1Secret/{sec.Data.Namespace()}/{sec.Data.Name()}";
                dict[key] = r;
            }
        }
        return dict.Values;
    }

    private bool IsVKProxyIngress(V1Ingress ingress)
    {
        if (ingress.Spec.IngressClassName is null)
        {
            return _isDefaultController;
        }

        lock (_sync)
        {
            return _ingressClassData.ContainsKey(ingress.Spec.IngressClassName);
        }
    }
}