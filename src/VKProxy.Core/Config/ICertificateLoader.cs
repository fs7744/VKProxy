using System.Security.Cryptography.X509Certificates;

namespace VKProxy.Core.Config;

public interface ICertificateLoader
{
    (X509Certificate2?, X509Certificate2Collection?) LoadCertificate(CertificateConfig? certInfo);
}