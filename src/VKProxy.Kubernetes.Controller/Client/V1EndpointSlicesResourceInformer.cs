using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace VKProxy.Kubernetes.Controller.Client;

internal class V1EndpointSlicesResourceInformer : ResourceInformer<V1EndpointSlice, V1EndpointSliceList>
{
    public V1EndpointSlicesResourceInformer(
        IKubernetes client,
        ResourceSelector<V1EndpointSlice> selector,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<V1EndpointsResourceInformer> logger)
        : base(client, selector, hostApplicationLifetime, logger)
    {
    }

    protected override Task<HttpOperationResponse<V1EndpointSliceList>> RetrieveResourceListAsync(bool? watch = null, string resourceVersion = null, ResourceSelector<V1EndpointSlice> resourceSelector = null, CancellationToken cancellationToken = default)
    {
        return Client.DiscoveryV1.ListEndpointSliceForAllNamespacesWithHttpMessagesAsync(watch: watch, resourceVersion: resourceVersion, fieldSelector: resourceSelector?.FieldSelector, cancellationToken: cancellationToken);
    }
}