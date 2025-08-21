using VKProxy.Kubernetes.Controller.Converters;

namespace VKProxy.Kubernetes.Controller.ConfigProvider;

public interface IUpdateConfig
{
    Task UpdateAsync(VKProxyConfigContext configContext, CancellationToken cancellationToken);
}