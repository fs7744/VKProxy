using System.Collections.Frozen;
using VKProxy.Kubernetes.Controller.Caching;

namespace VKProxy.Kubernetes.Controller.Services;

public struct ReconcileData
{
    public ReconcileData(IngressData ingress, List<ServiceData> services, List<Endpoints> endpoints, List<TlsSecret> tls)
    {
        Ingress = ingress;
        ServiceList = services;
        EndpointsList = endpoints;
        Tls = tls.ToFrozenDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IngressData Ingress { get; }
    public List<ServiceData> ServiceList { get; }
    public List<Endpoints> EndpointsList { get; }
    public IReadOnlyDictionary<string, TlsSecret> Tls { get; }
}