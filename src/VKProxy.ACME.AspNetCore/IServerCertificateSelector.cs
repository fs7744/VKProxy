using Microsoft.AspNetCore.Connections;
using System.Security.Cryptography.X509Certificates;

namespace VKProxy.ACME.AspNetCore;

public interface IServerCertificateSelector
{
    X509Certificate2? Select(ConnectionContext context, string? domainName);
}