using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using VKProxy.Core.Extensions;

namespace VKProxy.ACME.AspNetCore;

public class ServerCertificateSelector : IServerCertificateSelector
{
    private readonly ConcurrentDictionary<string, X509Certificate2> certs =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, X509Certificate2> challengeCerts =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<ServerCertificateSelector> logger;

    public ServerCertificateSelector(ILogger<ServerCertificateSelector> logger)
    {
        this.logger = logger;
    }

    public X509Certificate2? Select(ConnectionContext context, string? domainName)
    {
        if (domainName != null)
        {
            if (challengeCerts.TryGetValue(domainName, out X509Certificate2 cert))
            {
                return cert;
            }
            if (TryGetCertForDomain(domainName, out cert))
            {
                return cert;
            }
        }

        return null;
    }

    private X509Certificate2 AddWithDomainName(ConcurrentDictionary<string, X509Certificate2> certs, string domainName,
        X509Certificate2 certificate)
    {
        return certs.AddOrUpdate(
            domainName,
            certificate,
            (k, currentCert) =>
            {
                return certificate;
                //if (currentCert == null || certificate.NotAfter >= currentCert.NotAfter)
                //{
                //    return certificate;
                //}

                //return currentCert;
            });
    }

    public void Add(X509Certificate2 certificate)
    {
        foreach (var dnsName in certificate.GetAllDnsNames())
        {
            AddWithDomainName(certs, dnsName, certificate);
        }
        PreloadIntermediateCertificates(certificate);
    }

    public void AddChallengeCert(X509Certificate2 certificate)
    {
        foreach (var dnsName in certificate.GetAllDnsNames())
        {
            AddWithDomainName(challengeCerts, dnsName, certificate);
        }
    }

    private void PreloadIntermediateCertificates(X509Certificate2 certificate)
    {
        if (certificate.IsSelfSigned())
        {
            return;
        }

        using var chain = new X509Chain
        {
            ChainPolicy =
            {
                RevocationMode = X509RevocationMode.NoCheck
            }
        };

        var commonName = certificate.GetCommonName();
        try
        {
            if (chain.Build(certificate))
            {
                logger.LogDebug("Successfully tested certificate chain for {commonName}", commonName);
                return;
            }
        }
        catch (CryptographicException ex)
        {
            logger.LogDebug(ex, "Failed to validate certificate chain for {commonName}", commonName);
        }

        logger.LogWarning(
            "Failed to validate certificate for {commonName} ({thumbprint}). This could cause an outage of your app.",
            commonName, certificate.Thumbprint);
    }

    public bool HasCertForDomain(string domainName)
    {
        return TryGetCertForDomain(domainName, out _);
    }

    public bool TryGetCertForDomain(string domainName, out X509Certificate2 certificate)
    {
        if (certs.TryGetValue(domainName, out certificate))
        {
            return true;
        }
        var wildcardDomainName = certs.Keys.FirstOrDefault(n => n.StartsWith("*") && domainName.EndsWith(n[1..]));
        if (wildcardDomainName != null && certs.TryGetValue(wildcardDomainName, out certificate))
        {
            return true;
        }
        certificate = null;
        return false;
    }

    public void RemoveChallengeCert(X509Certificate2 cert)
    {
        foreach (var dnsName in cert.GetAllDnsNames())
        {
            RemoveChallengeCert(dnsName);
        }
    }

    public void RemoveChallengeCert(string domainName)
    {
        challengeCerts.TryRemove(domainName, out _);
    }

    public void OnSslAuthenticate(ConnectionContext context, SslServerAuthenticationOptions options)
    {
        if (challengeCerts.Count > 0)
        {
            (options.ApplicationProtocols ??= new List<SslApplicationProtocol>()).Add(TlsAlpn01DomainValidator.s_acmeTlsProtocol);
        }
    }
}