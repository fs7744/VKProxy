using Microsoft.AspNetCore.Connections;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace VKProxy.ACME.AspNetCore;

public interface IServerCertificateSelector : IServerCertificateSource
{
    X509Certificate2? Select(ConnectionContext context, string? domainName);

    void OnSslAuthenticate(ConnectionContext context, SslServerAuthenticationOptions options);
}

public interface IServerCertificateSource
{
    void Add(X509Certificate2 cert);

    void AddChallengeCert(X509Certificate2 cert);

    bool HasCertForDomain(string domainName);

    void RemoveChallengeCert(X509Certificate2 cert);

    bool TryGetCertForDomain(string domainName, out X509Certificate2 cert);
}