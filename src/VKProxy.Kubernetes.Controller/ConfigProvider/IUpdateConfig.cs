using VKProxy.Config;

namespace VKProxy.Kubernetes.Controller.ConfigProvider;

public interface IUpdateConfig
{
    Task UpdateAsync(IProxyConfig config, CancellationToken cancellationToken);
}