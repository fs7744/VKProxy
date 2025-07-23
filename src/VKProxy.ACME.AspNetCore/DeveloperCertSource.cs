using Microsoft.Extensions.Hosting;
using System.Security.Cryptography.X509Certificates;

namespace VKProxy.ACME.AspNetCore;

// see https://github.com/aspnet/Common/blob/61320f4ecc1a7b60e76ca8fe05cd86c98778f92c/shared/Microsoft.AspNetCore.Certificates.Generation.Sources/CertificateManager.cs#L19-L20
internal class DeveloperCertSource : ICertificateSource
{
    private const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
    private readonly IHostEnvironment environment;

    public DeveloperCertSource(IHostEnvironment environment)
    {
        this.environment = environment;
    }

    public async Task<IEnumerable<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment())
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByExtension, AspNetHttpsOid, validOnly: false);
            return certs;
            //foreach (var cert in certs.OrderByDescending(i => i.NotAfter))
            //{
            //    yield return cert;
            //}
        }
        return Enumerable.Empty<X509Certificate2>();
    }

    public Task SaveAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}