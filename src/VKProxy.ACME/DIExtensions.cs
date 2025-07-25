using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Text;
using System.Text.Json;
using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;
using VKProxy.Middlewares.Http;

namespace VKProxy.ACME;

public static class DIExtensions
{
    private const string DnsAcmePrefix = "_acme-challenge";

    public static string GetAcmeDnsDomain(this string domainName) =>
        $"{DnsAcmePrefix}.{domainName.TrimStart('*')}";

    public static IServiceCollection AddACME(this IServiceCollection services, Action<AcmeOptions> config = null)
    {
        var op = new AcmeOptions();
        config?.Invoke(op);
        services.AddSingleton(op);
        services.TryAddTransient<IAcmeContext, AcmeContext>();
        services.TryAddSingleton<IForwarderHttpClientFactory, ForwarderHttpClientFactory>();
        services.TryAddSingleton<IAcmeHttpClient, DefaultAcmeHttpClient>();
        services.TryAddSingleton<IAcmeClient, AcmeClient>();
        return services;
    }

    public static Key NewKey(this KeyAlgorithm algorithm, int? keySize = null)
    {
        return KeyAlgorithmProvider.NewKey(algorithm, keySize);
    }

    public static Task<IAccountContext> NewAccountAsync(this IAcmeContext context, string email, bool termsOfServiceAgreed, Key accountKey,
        string eabKeyId = null, string eabKey = null, string eabKeyAlg = null, CancellationToken cancellationToken = default)
    {
        return context.NewAccountAsync(new string[] { $"mailto:{email}" }, termsOfServiceAgreed, accountKey, eabKeyId, eabKey, eabKeyAlg, cancellationToken);
    }

    public static async Task<Order> FinalizeAsync(this IOrderContext context, CsrInfo csr, Key key, CancellationToken cancellationToken = default)
    {
        var builder = await context.CreateCsrAsync(key, cancellationToken);

        foreach (var (name, value) in csr.GetFields())
        {
            builder.AddName(name, value);
        }

        if (string.IsNullOrWhiteSpace(csr.CommonName))
        {
            builder.AddName("CN", builder.SubjectAlternativeNames[0]);
        }

        return await context.FinalizeAsync(builder.Generate(), cancellationToken);
    }

    public static async Task<CertificationRequestBuilder> CreateCsrAsync(this IOrderContext context, Key key, CancellationToken cancellationToken = default)
    {
        var builder = new CertificationRequestBuilder(key);
        var order = await context.GetResourceAsync(cancellationToken);
        foreach (var identifier in order.Identifiers)
        {
            builder.SubjectAlternativeNames.Add(identifier.Value);
        }

        return builder;
    }

    /// <summary>
    /// Finalizes and download the certifcate for the order.
    /// </summary>
    /// <param name="context">The order context.</param>
    /// <param name="csr">The CSR.</param>
    /// <param name="key">The private key for the certificate.</param>
    /// <param name="retryCount">Number of retries when the Order is in 'processing' state. (default = 1)</param>
    /// <param name="preferredChain">The preferred Root Certificate.</param>
    /// <returns>
    /// The certificate generated.
    /// </returns>
    public static async Task<CertificateChain> GenerateAsync(this IOrderContext context, CsrInfo csr, Key key, string preferredChain = null, CancellationToken cancellationToken = default)
    {
        var order = await context.GetResourceAsync(cancellationToken);
        if (order.Status != OrderStatus.Ready && // draft-11
            order.Status != OrderStatus.Pending) // pre draft-11
        {
            throw new AcmeException(string.Format("Can not finalize order with status '{0}'.", order.Status));
        }

        order = await context.FinalizeAsync(csr, key, cancellationToken);
        var retryCount = context.Context.RetryCount;
        while ((order == null || order.Status == OrderStatus.Processing) && retryCount-- > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(context.RetryAfter, 1)));
            order = await context.GetResourceAsync(cancellationToken);
        }

        if (order?.Status != OrderStatus.Valid)
        {
            throw new AcmeException("Fail to finalize order.");
        }

        return await context.DownloadAsync(preferredChain, cancellationToken);
    }

    /// <summary>
    /// Gets the authorization by identifier.
    /// </summary>
    /// <param name="context">The order context.</param>
    /// <param name="value">The identifier value.</param>
    /// <param name="type">The identifier type.</param>
    /// <returns>The authorization found.</returns>
    public static async Task<IAuthorizationContext> AuthorizationAsync(this IOrderContext context, string value, IdentifierType type = IdentifierType.Dns, CancellationToken cancellationToken = default)
    {
        var wildcard = value.StartsWith("*.");
        if (wildcard)
        {
            value = value.Substring(2);
        }

        await foreach (var authzCtx in context.GetAuthorizationsAsync(cancellationToken))
        {
            var authz = await authzCtx.GetResourceAsync(cancellationToken);
            if (string.Equals(authz.Identifier.Value, value, StringComparison.OrdinalIgnoreCase) &&
                wildcard == authz.Wildcard.GetValueOrDefault() &&
                authz.Identifier.Type == type)
            {
                return authzCtx;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts the certificate to PFX with the key.
    /// </summary>
    /// <param name="certificateChain">The certificate chain.</param>
    /// <param name="certKey">The certificate private key.</param>
    /// <returns>The PFX.</returns>
    public static PfxBuilder ToPfx(this CertificateChain certificateChain, Key certKey)
    {
        var pfx = new PfxBuilder(certificateChain.Certificate.ToDer(), certKey);
        if (certificateChain.Issuers != null)
        {
            foreach (var issuer in certificateChain.Issuers)
            {
                pfx.AddIssuer(issuer.ToDer());
            }
        }

        return pfx;
    }

    /// <summary>
    /// Encodes the full certificate chain in PEM.
    /// </summary>
    /// <param name="certificateChain">The certificate chain.</param>
    /// <param name="certKey">The certificate key.</param>
    /// <returns>The encoded certificate chain.</returns>
    public static string ToPem(this CertificateChain certificateChain, Key certKey = null)
    {
        var certStore = new CertificateStore();
        foreach (var issuer in certificateChain.Issuers)
        {
            certStore.Add(issuer.ToDer());
        }

        var issuers = certStore.GetIssuers(certificateChain.Certificate.ToDer());

        using (var writer = new StringWriter())
        {
            if (certKey != null)
            {
                writer.WriteLine(certKey.ToPem().TrimEnd());
            }

            writer.WriteLine(certificateChain.Certificate.ToPem().TrimEnd());

            var certParser = new X509CertificateParser();
            var pemWriter = new PemWriter(writer);
            foreach (var issuer in issuers)
            {
                var cert = certParser.ReadCertificate(issuer);
                pemWriter.WriteObject(cert);
            }

            return writer.ToString();
        }
    }

    /// <summary>
    /// Saves the key pair to the specified stream.
    /// </summary>
    /// <param name="keyInfo">The key information.</param>
    /// <param name="stream">The stream.</param>
    public static void Save(this KeyInfo keyInfo, Stream stream)
    {
        var keyPair = keyInfo.CreateKeyPair();
        using (var writer = new StreamWriter(stream))
        {
            var pemWriter = new PemWriter(writer);
            pemWriter.WriteObject(keyPair);
        }
    }

    /// <summary>
    /// Gets the key pair.
    /// </summary>
    /// <param name="keyInfo">The key data.</param>
    /// <returns>The key pair</returns>
    public static AsymmetricCipherKeyPair CreateKeyPair(this KeyInfo keyInfo)
    {
        var (_, keyPair) = KeyAlgorithmProvider.GetKeyPair(keyInfo.PrivateKeyInfo);
        return keyPair;
    }

    /// <summary>
    /// Exports the key pair.
    /// </summary>
    /// <param name="keyPair">The key pair.</param>
    /// <returns>The key data.</returns>
    public static KeyInfo Export(this AsymmetricCipherKeyPair keyPair)
    {
        var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);

        return new KeyInfo
        {
            PrivateKeyInfo = privateKey.GetDerEncoded()
        };
    }

    private static readonly DerObjectIdentifier acmeValidationV1Id = new DerObjectIdentifier("1.3.6.1.5.5.7.1.31");

    /// <summary>
    /// Generates the thumbprint for the given account <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The account key.</param>
    /// <returns>The thumbprint.</returns>
    internal static byte[] GenerateThumbprint(this Key key)
    {
        var jwk = key.JsonWebKey;
        var json = JsonSerializer.Serialize(jwk, DefaultAcmeHttpClient.JsonSerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashed = DigestUtilities.CalculateDigest("SHA256", bytes);

        return hashed;
    }

    /// <summary>
    /// Generates the base64 encoded thumbprint for the given account <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The account key.</param>
    /// <returns>The thumbprint.</returns>
    public static string Thumbprint(this Key key)
    {
        var jwkThumbprint = key.GenerateThumbprint();
        return JwsConvert.ToBase64String(jwkThumbprint);
    }

    /// <summary>
    /// Generates key authorization string.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="token">The challenge token.</param>
    /// <returns>The key authorization string.</returns>
    public static string KeyAuthorization(this Key key, string token)
    {
        var jwkThumbprintEncoded = key.Thumbprint();
        return $"{token}.{jwkThumbprintEncoded}";
    }

    /// <summary>
    /// Generates the value for DNS TXT record.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="token">The challenge token.</param>
    /// <returns>The DNS text value for dns-01 validation.</returns>
    public static string DnsTxt(this Key key, string token)
    {
        var keyAuthz = key.KeyAuthorization(token);
        var hashed = DigestUtilities.CalculateDigest("SHA256", Encoding.UTF8.GetBytes(keyAuthz));
        return JwsConvert.ToBase64String(hashed);
    }

    /// <summary>
    /// Generates the certificate for <see cref="ChallengeTypes.TlsAlpn01" /> validation.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="token">The <see cref="ChallengeTypes.TlsAlpn01" /> token.</param>
    /// <param name="subjectName">Name of the subject.</param>
    /// <param name="certificateKey">The certificate key pair.</param>
    /// <returns>The tls-alpn-01 certificate in PEM.</returns>
    public static string TlsAlpnCertificate(this Key key, string token, string subjectName, Key certificateKey)
    {
        var keyAuthz = key.KeyAuthorization(token);
        var hashed = DigestUtilities.CalculateDigest("SHA256", Encoding.UTF8.GetBytes(keyAuthz));

        var (_, keyPair) = KeyAlgorithmProvider.GetKeyPair(certificateKey.ToDer());

        var signatureFactory = new Asn1SignatureFactory(certificateKey.Algorithm.ToPkcsObjectId(), keyPair.Private, new SecureRandom());
        var gen = new X509V3CertificateGenerator();
        var certName = new X509Name($"CN={subjectName}");
        var serialNo = BigInteger.ProbablePrime(120, new SecureRandom());

        gen.SetSerialNumber(serialNo);
        gen.SetSubjectDN(certName);
        gen.SetIssuerDN(certName);
        gen.SetNotBefore(DateTime.UtcNow);
        gen.SetNotAfter(DateTime.UtcNow.AddDays(7));
        gen.SetPublicKey(keyPair.Public);

        // SAN for validation
        var gns = new[] { new GeneralName(GeneralName.DnsName, subjectName) };
        gen.AddExtension(X509Extensions.SubjectAlternativeName.Id, false, new GeneralNames(gns));

        // ACME-TLS/1
        gen.AddExtension(
            acmeValidationV1Id,
            true,
            hashed);

        var newCert = gen.Generate(signatureFactory);

        using (var sr = new StringWriter())
        {
            var pemWriter = new PemWriter(sr);
            pemWriter.WriteObject(newCert);
            return sr.ToString();
        }
    }

    /// <summary>
    /// Gets the HTTP challenge.
    /// </summary>
    /// <param name="authorizationContext">The authorization context.</param>
    /// <returns>The HTTP challenge, <c>null</c> if no HTTP challenge available.</returns>
    public static Task<IChallengeContext> HttpAsync(this IAuthorizationContext authorizationContext, CancellationToken cancellationToken = default) =>
        authorizationContext.ChallengeAsync(Challenge.Http01, cancellationToken);

    /// <summary>
    /// Gets the DNS challenge.
    /// </summary>
    /// <param name="authorizationContext">The authorization context.</param>
    /// <returns>The DNS challenge, <c>null</c> if no DNS challenge available.</returns>
    public static Task<IChallengeContext> DnsAsync(this IAuthorizationContext authorizationContext, CancellationToken cancellationToken = default) =>
        authorizationContext.ChallengeAsync(Challenge.Dns01, cancellationToken);

    /// <summary>
    /// Gets the TLS ALPN challenge.
    /// </summary>
    /// <param name="authorizationContext">The authorization context.</param>
    /// <returns>The TLS ALPN challenge, <c>null</c> if no TLS ALPN challenge available.</returns>
    public static Task<IChallengeContext> TlsAlpnAsync(this IAuthorizationContext authorizationContext, CancellationToken cancellationToken = default) =>
        authorizationContext.ChallengeAsync(Challenge.TlsAlpn01, cancellationToken);

    /// <summary>
    /// Gets a challenge by type.
    /// </summary>
    /// <param name="authorizationContext">The authorization context.</param>
    /// <param name="type">The challenge type.</param>
    /// <returns>The challenge, <c>null</c> if no challenge found.</returns>
    public static async Task<IChallengeContext> ChallengeAsync(this IAuthorizationContext authorizationContext, string type, CancellationToken cancellationToken = default)
    {
        await foreach (var item in authorizationContext.GetChallengesAsync(cancellationToken))
        {
            if (type.Equals(item.Type, StringComparison.OrdinalIgnoreCase))
                return item;
        }
        return null;
    }

    public static IOrderContext Order(this IAcmeContext context, Uri orderLocation)
    {
        return new OrderContext(context, orderLocation);
    }
}