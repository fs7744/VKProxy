using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace VKProxy.Core.Config;

public class CertificateLoader : ICertificateLoader
{
    private readonly IHostEnvironment hostEnvironment;

    public CertificateLoader(IHostEnvironment hostEnvironment)
    {
        this.hostEnvironment = hostEnvironment;
    }

    public (X509Certificate2?, X509Certificate2Collection?) LoadCertificate(CertificateConfig? certInfo)
    {
        if (certInfo is null)
        {
            return (null, null);
        }
        else if (certInfo.IsFileCert)
        {
            var certificatePath = Path.Combine(hostEnvironment.ContentRootPath, certInfo.Path!);
            var fullChain = new X509Certificate2Collection();
            fullChain.ImportFromPemFile(certificatePath);

            if (certInfo.KeyPath != null)
            {
                var certificateKeyPath = Path.Combine(hostEnvironment.ContentRootPath, certInfo.KeyPath);
                var certificate = GetCertificate(certificatePath);

                if (certificate != null)
                {
                    certificate = LoadCertificateKey(certificate, certificateKeyPath, certInfo.Password);
                }

                if (certificate != null)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        return (PersistKey(certificate), fullChain);
                    }

                    return (certificate, fullChain);
                }

                throw new InvalidOperationException("The provided key file is missing or invalid.");
            }

            return (new X509Certificate2(Path.Combine(hostEnvironment.ContentRootPath, certInfo.Path!), certInfo.Password), fullChain);
        }
        else if (certInfo.IsStoreCert)
        {
            return (LoadFromStoreCert(certInfo), null);
        }
        else
        {
            return (null, null);
        }
    }

    private static X509Certificate2 PersistKey(X509Certificate2 fullCertificate)
    {
        // We need to force the key to be persisted.
        // See https://github.com/dotnet/runtime/issues/23749
        var certificateBytes = fullCertificate.Export(X509ContentType.Pkcs12, "");
        return new X509Certificate2(certificateBytes, "", X509KeyStorageFlags.DefaultKeySet);
    }

    private static X509Certificate2 LoadCertificateKey(X509Certificate2 certificate, string keyPath, string? password)
    {
        // OIDs for the certificate key types.
        const string RSAOid = "1.2.840.113549.1.1.1";
        const string DSAOid = "1.2.840.10040.4.1";
        const string ECDsaOid = "1.2.840.10045.2.1";

        // Duplication is required here because there are separate CopyWithPrivateKey methods for each algorithm.
        var keyText = File.ReadAllText(keyPath);
        switch (certificate.PublicKey.Oid.Value)
        {
            case RSAOid:
                {
                    using var rsa = RSA.Create();
                    ImportKeyFromFile(rsa, keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(rsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            case ECDsaOid:
                {
                    using var ecdsa = ECDsa.Create();
                    ImportKeyFromFile(ecdsa, keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(ecdsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            case DSAOid:
                {
                    using var dsa = DSA.Create();
                    ImportKeyFromFile(dsa, keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(dsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            default:
                throw new InvalidOperationException($"Unknown algorithm for certificate with public key type '{certificate.PublicKey.Oid.Value}'.");
        }
    }

    private static InvalidOperationException CreateErrorGettingPrivateKeyException(string keyPath, Exception ex)
    {
        return new InvalidOperationException($"Error getting private key from '{keyPath}'.", ex);
    }

    private static X509Certificate2? GetCertificate(string certificatePath)
    {
        if (X509Certificate2.GetCertContentType(certificatePath) == X509ContentType.Cert)
        {
            return new X509Certificate2(certificatePath);
        }

        return null;
    }

    private static void ImportKeyFromFile(AsymmetricAlgorithm asymmetricAlgorithm, string keyText, string? password)
    {
        if (password == null)
        {
            asymmetricAlgorithm.ImportFromPem(keyText);
        }
        else
        {
            asymmetricAlgorithm.ImportFromEncryptedPem(keyText, password);
        }
    }

    private static X509Certificate2 LoadFromStoreCert(CertificateConfig certInfo)
    {
        var subject = certInfo.Subject!;
        var storeName = string.IsNullOrEmpty(certInfo.Store) ? StoreName.My.ToString() : certInfo.Store;
        var location = certInfo.Location;
        var storeLocation = StoreLocation.CurrentUser;
        if (!string.IsNullOrEmpty(location))
        {
            storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), location, ignoreCase: true);
        }
        var allowInvalid = certInfo.AllowInvalid ?? false;

        return CertificateLoader.LoadFromStoreCert(subject, storeName, storeLocation, allowInvalid);
    }

    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    public static X509Certificate2 LoadFromStoreCert(string subject, string storeName, StoreLocation storeLocation, bool allowInvalid)
    {
        using (var store = new X509Store(storeName, storeLocation))
        {
            X509Certificate2Collection? storeCertificates = null;
            X509Certificate2? foundCertificate = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
                storeCertificates = store.Certificates;
                foreach (var certificate in storeCertificates.Find(X509FindType.FindBySubjectName, subject, !allowInvalid)
                    .OfType<X509Certificate2>()
                    .Where(IsCertificateAllowedForServerAuth)
                    .Where(DoesCertificateHaveAnAccessiblePrivateKey)
                    .OrderByDescending(certificate => certificate.NotAfter))
                {
                    // Pick the first one if there's no exact match as a fallback to substring default.
                    foundCertificate ??= certificate;

                    if (certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false).Equals(subject, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foundCertificate = certificate;
                        break;
                    }
                }

                if (foundCertificate == null)
                {
                    throw new InvalidOperationException($"The requested certificate {subject} could not be found in {storeLocation}/{storeName} with AllowInvalid setting: {allowInvalid}.");
                }

                return foundCertificate;
            }
            finally
            {
                DisposeCertificates(storeCertificates, except: foundCertificate);
            }
        }
    }

    internal static bool IsCertificateAllowedForServerAuth(X509Certificate2 certificate)
    {
        /* If the Extended Key Usage extension is included, then we check that the serverAuth usage is included. (http://oid-info.com/get/1.3.6.1.5.5.7.3.1)
         * If the Extended Key Usage extension is not included, then we assume the certificate is allowed for all usages.
         *
         * See also https://blogs.msdn.microsoft.com/kaushal/2012/02/17/client-certificates-vs-server-certificates/
         *
         * From https://tools.ietf.org/html/rfc3280#section-4.2.1.13 "Certificate Extensions: Extended Key Usage"
         *
         * If the (Extended Key Usage) extension is present, then the certificate MUST only be used
         * for one of the purposes indicated.  If multiple purposes are
         * indicated the application need not recognize all purposes indicated,
         * as long as the intended purpose is present.  Certificate using
         * applications MAY require that a particular purpose be indicated in
         * order for the certificate to be acceptable to that application.
         */

        var hasEkuExtension = false;

        foreach (var extension in certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>())
        {
            hasEkuExtension = true;
            foreach (var oid in extension.EnhancedKeyUsages)
            {
                if (string.Equals(oid.Value, ServerAuthenticationOid, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return !hasEkuExtension;
    }

    internal static bool DoesCertificateHaveAnAccessiblePrivateKey(X509Certificate2 certificate)
        => certificate.HasPrivateKey;

    internal static bool DoesCertificateHaveASubjectAlternativeName(X509Certificate2 certificate)
        => certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().Any();

    private static void DisposeCertificates(X509Certificate2Collection? certificates, X509Certificate2? except)
    {
        if (certificates != null)
        {
            foreach (var certificate in certificates)
            {
                if (!certificate.Equals(except))
                {
                    certificate.Dispose();
                }
            }
        }
    }
}