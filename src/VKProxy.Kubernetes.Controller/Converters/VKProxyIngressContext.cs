using VKProxy.Kubernetes.Controller.Caching;

namespace VKProxy.Kubernetes.Controller.Converters;

public sealed class VKProxyIngressContext
{
    public VKProxyIngressContext(IngressData ingress, List<ServiceData> services, List<Endpoints> endpoints)
    {
        Ingress = ingress;
        Services = services;
        Endpoints = endpoints;
    }

    public VKProxyIngressOptions Options { get; set; } = new VKProxyIngressOptions();
    public IngressData Ingress { get; }

    public List<ServiceData> Services { get; }
    public List<Endpoints> Endpoints { get; }
}