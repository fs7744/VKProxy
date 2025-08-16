using k8s.Models;

namespace VKProxy.Kubernetes.Controller.Caching;

public struct Endpoints
{
    public Endpoints(V1Endpoints endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        Name = endpoints.Name();
        Subsets = endpoints.Subsets;
    }

    public string Name { get; set; }
    public IList<V1EndpointSubset> Subsets { get; }
}