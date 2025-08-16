using k8s.Models;

namespace VKProxy.Kubernetes.Controller.Caching;

public struct IngressData
{
    public IngressData(V1Ingress ingress)
    {
        ArgumentNullException.ThrowIfNull(ingress);

        Spec = ingress.Spec;
        Metadata = ingress.Metadata;
    }

    public V1IngressSpec Spec { get; set; }
    public V1ObjectMeta Metadata { get; set; }
}