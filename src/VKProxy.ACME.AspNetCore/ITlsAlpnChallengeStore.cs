using System.Security.Cryptography.X509Certificates;

namespace VKProxy.ACME.AspNetCore;

public interface ITlsAlpnChallengeStore
{
    Task AddChallengeAsync(string domainName, X509Certificate2 cert, CancellationToken cancellationToken);

    Task RemoveChallengeAsync(string domainName, X509Certificate2 cert, CancellationToken cancellationToken);
}

public class TlsAlpnChallengeStore : ITlsAlpnChallengeStore
{
    private readonly IServerCertificateSource selector;

    public TlsAlpnChallengeStore(IServerCertificateSource selector)
    {
        this.selector = selector;
    }

    public Task AddChallengeAsync(string domainName, X509Certificate2 cert, CancellationToken cancellationToken)
    {
        selector.AddChallengeCert(cert);
        return Task.CompletedTask;
    }

    public Task RemoveChallengeAsync(string domainName, X509Certificate2 cert, CancellationToken cancellationToken)
    {
        selector.RemoveChallengeCert(cert);
        return Task.CompletedTask;
    }
}