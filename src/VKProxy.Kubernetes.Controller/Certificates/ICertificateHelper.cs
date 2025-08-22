using k8s.Models;
using VKProxy.Core.Config;

namespace VKProxy.Kubernetes.Controller.Certificates;

public interface ICertificateHelper
{
    CertificateConfig ConvertHttpsConfig(NamespacedName namespacedName, V1Secret secret);
}