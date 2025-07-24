using Microsoft.AspNetCore.Connections;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace VKProxy.ACME.AspNetCore;

public interface IServerCertificateSelector
{
    X509Certificate2? Select(ConnectionContext context, string? domainName);

    void OnSslAuthenticate(ConnectionContext context, SslServerAuthenticationOptions options);
}