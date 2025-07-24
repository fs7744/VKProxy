using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using VKProxy.Core.Extensions;

namespace VKProxy.ACME.AspNetCore;

public class X509CertStoreSource : ICertificateSource, IDisposable
{
    private readonly X509Store store;
    private readonly ILogger<X509CertStoreSource> logger;
    private readonly AcmeChallengeOptions options;

    public X509CertStoreSource(ILogger<X509CertStoreSource> logger, AcmeChallengeOptions options)
    {
        store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        this.logger = logger;
        this.options = options;
    }

    public void Dispose()
    {
        store.Close();
    }

    public async Task<IEnumerable<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken)
    {
        var domainNames = new HashSet<string>(options.DomainNames);
        var certs = store.Certificates.Find(X509FindType.FindByTimeValid,
            DateTime.Now,
            validOnly: true);
        return certs.Where(x => x.HasPrivateKey && x.GetAllDnsNames().Any(domainNames.Contains));
    }

    public Task SaveAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        try
        {
            store.Add(certificate);
        }
        catch (Exception ex)
        {
            logger.LogError(0, ex, "Failed to save certificate to store");
            throw;
        }

        return Task.CompletedTask;
    }
}