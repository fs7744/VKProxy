using VKProxy.Config;

namespace VKProxy.Kubernetes.Controller.ConfigProvider;

public interface IUpdateConfig
{
    Task UpdateAsync(IReadOnlyProxyConfig config, CancellationToken cancellationToken);
}