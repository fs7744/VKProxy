using k8s.Models;

namespace VKProxy.Kubernetes.Controller.Caching;

public struct Endpoints
{
    public Endpoints(V1EndpointSlice endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        Name = endpoints.Name();
        this.EndpointList = endpoints.Endpoints;
        Ports = endpoints.Ports;
    }

    public string Name { get; set; }
    public IList<V1Endpoint> EndpointList { get; }
    public IList<Discoveryv1EndpointPort> Ports { get; }
}