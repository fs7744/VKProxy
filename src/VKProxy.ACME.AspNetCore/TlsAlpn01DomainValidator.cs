using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VKProxy.Core.Config;

namespace VKProxy.ACME.AspNetCore;

public class TlsAlpn01DomainValidator : DomainOwnershipValidator
{// See RFC8737 section 6.1
    private static readonly Oid s_acmeExtensionOid = new("1.3.6.1.5.5.7.1.31");
    private const string ProtocolName = "acme-tls/1";
    internal static readonly SslApplicationProtocol s_acmeTlsProtocol = new(ProtocolName);
    private readonly ITlsAlpnChallengeStore challengeStore;
    private readonly ILogger<TlsAlpn01DomainValidator> logger;

    public TlsAlpn01DomainValidator(ITlsAlpnChallengeStore challengeStore, ILogger<TlsAlpn01DomainValidator> logger)
    {
        this.challengeStore = challengeStore;
        this.logger = logger;
    }

    public override async Task ValidateOwnershipAsync(string domainName, AcmeStateContext context, IAuthorizationContext authzContext, CancellationToken cancellationToken)
    {
        context.Logger.LogDebug("Validate TlsAlpn01 for {domainName}", domainName);
        cancellationToken.ThrowIfCancellationRequested();
        var tlsChallenge = await authzContext.TlsAlpnAsync(cancellationToken) ?? throw new AcmeException(
                "Did not receive challenge information for challenge type TlsAlpn01");
        var certificate = PrepareChallengeCert(domainName, tlsChallenge.KeyAuthz);

        try
        {
            await challengeStore.AddChallengeAsync(domainName, certificate, cancellationToken);
            await tlsChallenge.ValidateAsync(cancellationToken);
            await WaitForChallengeResultAsync(domainName, context, authzContext, cancellationToken);
        }
        finally
        {
            if (certificate != null)
            {
                await challengeStore.RemoveChallengeAsync(domainName, certificate, cancellationToken);
            }
        }
    }

    public X509Certificate2 PrepareChallengeCert(string domainName, string keyAuthorization)
    {
        logger.LogDebug("Creating ALPN self-signed cert for {domainName} and key authz {keyAuth}",
            domainName, keyAuthorization);
        var key = RSA.Create(2048);
        var csr = new CertificateRequest(
            "CN=" + domainName,
            key,
            HashAlgorithmName.SHA512,
            RSASignaturePadding.Pkcs1);

        /*
        RFC 8737 Section 3

        The client prepares for validation by constructing a self-signed
        certificate that MUST contain an acmeIdentifier extension and a
        subjectAlternativeName extension [RFC5280].  The
        subjectAlternativeName extension MUST contain a single dNSName entry
        where the value is the domain name being validated.  The
        acmeIdentifier extension MUST contain the SHA-256 digest [FIPS180-4]
        of the key authorization [RFC8555] for the challenge.  The
        acmeIdentifier extension MUST be critical so that the certificate
        isn't inadvertently used by non-ACME software.
        */

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(domainName);
        csr.CertificateExtensions.Add(sanBuilder.Build());

        // adds acmeIdentifier extension (critical = true)
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyAuthorization));
        var extensionData = new DerOctetString(hash).GetDerEncoded();
        var acmeIdentifierExtension = new X509Extension(s_acmeExtensionOid, extensionData, critical: true);
        csr.CertificateExtensions.Add(acmeIdentifierExtension);

        // This cert is ephemeral and does not need to be stored for reuse later
        var cert = csr.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        return cert;
    }
}