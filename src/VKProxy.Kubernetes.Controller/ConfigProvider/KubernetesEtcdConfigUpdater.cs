using VKProxy.Config;

namespace VKProxy.Kubernetes.Controller.ConfigProvider;

internal class KubernetesEtcdConfigUpdater : IUpdateConfig
{
    //todo diff etcd config change and update
    public Task UpdateAsync(IReadOnlyProxyConfig config, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}