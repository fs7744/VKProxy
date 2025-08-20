using Microsoft.Extensions.Logging;
using VKProxy.Kubernetes.Controller.Caching;
using VKProxy.Kubernetes.Controller.Client;
using VKProxy.Kubernetes.Controller.Converters;

namespace VKProxy.Kubernetes.Controller.Services;

/// <summary>
/// IReconciler is a service interface called by the <see cref="IngressController"/> to process
/// the work items as they are dequeued.
/// </summary>
public partial class Reconciler : IReconciler
{
    private readonly IIngressResourceStatusUpdater _ingressResourceStatusUpdater;
    private readonly ILogger<Reconciler> _logger;

    public Reconciler(IIngressResourceStatusUpdater ingressResourceStatusUpdater, ILogger<Reconciler> logger)
    {
        ArgumentNullException.ThrowIfNull(ingressResourceStatusUpdater);

        _ingressResourceStatusUpdater = ingressResourceStatusUpdater;
        _logger = logger;
    }

    public async Task ProcessAsync(IEnumerable<IK8SChange> changes, CancellationToken cancellationToken)
    {
        try
        {
            // todo
            //var ingresses = _cache.GetIngresses().ToArray();

            //var configContext = new VKProxyConfigContext();

            //foreach (var ingress in ingresses)
            //{
            //    try
            //    {
            //        if (_cache.TryGetReconcileData(new NamespacedName(ingress.Metadata.NamespaceProperty, ingress.Metadata.Name), out var data))
            //        {
            //            var ingressContext = new VKProxyIngressContext(ingress, data.ServiceList, data.EndpointsList);
            //            VKProxyParser.ConvertFromKubernetesIngress(ingressContext, configContext);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogWarning(ex, "Uncaught exception occurred while reconciling ingress {IngressNamespace}/{IngressName}", ingress.Metadata.NamespaceProperty, ingress.Metadata.Name);
            //    }
            //}

            //var clusters = configContext.BuildClusterConfig();

            //_logger.LogInformation(JsonSerializer.Serialize(configContext.Routes));
            //_logger.LogInformation(JsonSerializer.Serialize(clusters));

            //await _updateConfig.UpdateAsync(configContext.Routes, clusters, cancellationToken).ConfigureAwait(false);
            await _ingressResourceStatusUpdater.UpdateStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Uncaught exception occurred while reconciling");
            throw;
        }
    }
}