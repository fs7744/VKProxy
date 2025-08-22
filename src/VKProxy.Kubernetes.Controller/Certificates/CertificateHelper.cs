using k8s.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VKProxy.Core.Config;

namespace VKProxy.Kubernetes.Controller.Certificates;

public class CertificateHelper : ICertificateHelper
{
    private const string TlsCertKey = "tls.crt";
    private const string TlsPrivateKeyKey = "tls.key";
    private const string TlsPassword = "password";

    private readonly ILogger<CertificateHelper> _logger;
    private readonly ICertificateLoader loader;

    public CertificateHelper(ILogger<CertificateHelper> logger, ICertificateLoader loader)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        this.loader = loader;
    }

    public CertificateConfig ConvertHttpsConfig(NamespacedName namespacedName, V1Secret secret)
    {
        var c = new CertificateConfig
        {
            PEM = secret?.StringData?[TlsCertKey],
            PEMKey = secret?.StringData?[TlsPrivateKeyKey],
            Password = secret?.StringData?[TlsPassword]
        };

        if (c.PEM == null)
        {
            var cert = secret?.Data?[TlsCertKey];
            var privateKey = secret?.Data?[TlsPrivateKeyKey];
            var password = secret?.Data?[TlsPassword];
            c.PEM = cert == null ? null : EnsurePemFormat(cert, "CERTIFICATE");
            c.Password = password == null ? null : Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.ASCII.GetString(password)));
            c.PEMKey = privateKey == null ? null : EnsurePemFormat(privateKey, password != null ? "ENCRYPTED PRIVATE KEY" : "PRIVATE KEY");
        }

        try
        {
            var (p, cc) = loader.LoadCertificate(c);
            if (p == null)
                return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert secret '{NamespacedName}'", namespacedName);
            return null;
        }
        return c;
    }

    /// <summary>
    /// Kubernetes Secrets should be stored in base-64 encoded DER format (see https://kubernetes.io/docs/concepts/configuration/secret/#tls-secrets)
    /// but need can be imported into a <see cref="X509Certificate2"/> object via PEM. Before this type of secret existed, an Opaque secret would be
    /// used containing the full PEM format, so it's possible that the incorrect format would be used.
    /// Doing it this way means we are more tolerant in handling certs in the wrong format.
    /// </summary>
    /// <param name="data">The raw data.</param>
    /// <param name="pemType">The type for the PEM header.</param>
    /// <returns>The certificate data in PEM format.</returns>
    private static string EnsurePemFormat(byte[] data, string pemType)
    {
        var der = Encoding.ASCII.GetString(data);
        if (!der.StartsWith("---", StringComparison.Ordinal))
        {
            // Convert from encoded DER to PEM
            return $"-----BEGIN {pemType}-----\n{der}\n-----END {pemType}-----";
        }

        return der;
    }
}