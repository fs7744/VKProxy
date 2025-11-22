using Lmzzz.AspNetCoreTemplate;
using VKProxy.Kubernetes.Controller.Caching;

namespace VKProxy.Kubernetes.Controller.Converters;

public sealed class VKProxyIngressContext
{
    public VKProxyIngressContext(IngressData ingress, List<ServiceData> services, List<Endpoints> endpoints, IReadOnlyDictionary<string, TlsSecret> tls)
    {
        Ingress = ingress;
        Services = services;
        Endpoints = endpoints;
        Tls = tls;
    }

    public VKProxyIngressOptions Options { get; set; } = new VKProxyIngressOptions();
    public IngressData Ingress { get; }

    public List<ServiceData> Services { get; }
    public List<Endpoints> Endpoints { get; }
    public IReadOnlyDictionary<string, TlsSecret> Tls { get; }
    public ITemplateEngineFactory StatementFactory { get; set; }
}