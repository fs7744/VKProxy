using VKProxy.Kubernetes.Controller.Converters;

namespace VKProxy.Kubernetes.Controller.ConfigProvider;

internal class KubernetesEtcdConfigUpdater : IUpdateConfig
{
    //todo diff etcd config change and update
    public Task UpdateAsync(VKProxyConfigContext configContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}