using k8s.Models;

namespace VKProxy.Kubernetes.Controller.Caching;

public struct ServiceData
{
    public ServiceData(V1Service service)
    {
        ArgumentNullException.ThrowIfNull(service);

        Spec = service.Spec;
        Metadata = service.Metadata;
    }

    public V1ServiceSpec Spec { get; set; }
    public V1ObjectMeta Metadata { get; set; }
}