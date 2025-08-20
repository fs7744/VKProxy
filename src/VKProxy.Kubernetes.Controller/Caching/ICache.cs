using k8s;
using k8s.Models;
using System.Collections.Immutable;
using VKProxy.Kubernetes.Controller.Services;

namespace VKProxy.Kubernetes.Controller.Caching;

public interface ICache
{
    void Update(WatchEventType eventType, V1IngressClass ingressClass);

    bool Update(WatchEventType eventType, V1Ingress ingress);

    ImmutableList<string> Update(WatchEventType eventType, V1Service service);

    ImmutableList<string> Update(WatchEventType eventType, V1EndpointSlice endpoints);

    void Update(WatchEventType eventType, V1Secret secret);

    bool TryGetReconcileData(NamespacedName key, out ReconcileData data);

    void GetKeys(List<NamespacedName> keys);

    IEnumerable<IngressData> GetIngresses();
}