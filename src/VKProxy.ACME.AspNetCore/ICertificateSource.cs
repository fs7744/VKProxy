using System.Security.Cryptography.X509Certificates;

namespace VKProxy.ACME.AspNetCore;

public interface ICertificateSource
{
    Task<IEnumerable<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken);

    Task SaveAsync(X509Certificate2 certificate, CancellationToken cancellationToken);
}