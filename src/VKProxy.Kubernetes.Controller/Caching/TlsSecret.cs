using k8s.Models;
using VKProxy.Core.Config;

namespace VKProxy.Kubernetes.Controller.Caching;

public struct TlsSecret
{
    public TlsSecret(V1Secret secret, CertificateConfig certificate)
    {
        Name = secret.Name();
        Secret = secret;
        Certificate = certificate;
    }

    public string Name { get; }
    public V1Secret Secret { get; }
    public CertificateConfig Certificate { get; }
}