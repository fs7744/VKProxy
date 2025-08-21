using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using VKProxy.Config;
using VKProxy.HttpRoutingStatement;
using VKProxy.Kubernetes.Controller.Caching;
using VKProxy.Kubernetes.Controller.Client;
using VKProxy.Kubernetes.Controller.ConfigProvider;
using VKProxy.Kubernetes.Controller.Converters;

namespace VKProxy.Kubernetes.Controller.Services;

/// <summary>
/// IReconciler is a service interface called by the <see cref="IngressController"/> to process
/// the work items as they are dequeued.
/// </summary>
public partial class Reconciler : IReconciler
{
    private readonly IIngressResourceStatusUpdater _ingressResourceStatusUpdater;
    private readonly ICache _cache;
    private readonly ILogger<Reconciler> _logger;
    private readonly IRouteStatementFactory statementFactory;
    private readonly IUpdateConfig _updateConfig;

    public Reconciler(IIngressResourceStatusUpdater ingressResourceStatusUpdater, ICache cache, IUpdateConfig updateConfig, ILogger<Reconciler> logger, IRouteStatementFactory statementFactory)
    {
        ArgumentNullException.ThrowIfNull(ingressResourceStatusUpdater);

        _ingressResourceStatusUpdater = ingressResourceStatusUpdater;
        this._cache = cache;
        _logger = logger;
        this.statementFactory = statementFactory;
        _updateConfig = updateConfig;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ingresses = _cache.GetIngresses().ToArray();

            var configContext = new VKProxyConfigContext();

            foreach (var ingress in ingresses)
            {
                try
                {
                    if (_cache.TryGetReconcileData(new NamespacedName(ingress.Metadata.NamespaceProperty, ingress.Metadata.Name), out var data))
                    {
                        var ingressContext = new VKProxyIngressContext(ingress, data.ServiceList, data.EndpointsList) { StatementFactory = statementFactory };
                        VKProxyParser.ConvertFromKubernetesIngress(ingressContext, configContext);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Uncaught exception occurred while reconciling ingress {IngressNamespace}/{IngressName}", ingress.Metadata.NamespaceProperty, ingress.Metadata.Name);
                }
            }

            await _updateConfig.UpdateAsync(configContext, cancellationToken).ConfigureAwait(false);
            await _ingressResourceStatusUpdater.UpdateStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Uncaught exception occurred while reconciling");
            throw;
        }
    }
}