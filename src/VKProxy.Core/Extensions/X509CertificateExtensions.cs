using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace VKProxy.Core.Extensions;

public static class X509CertificateExtensions
{
    internal const string Oid = "2.5.29.17";
    private static readonly char delimiter;
    private static readonly string identifier;
    private static readonly string separator;

    static X509CertificateExtensions()
    {
        // Extracted a well-known X509Extension
        var x509ExtensionBytes = new byte[] {
                    48, 36, 130, 21, 110, 111, 116, 45, 114, 101, 97, 108, 45, 115, 117, 98, 106, 101, 99,
                    116, 45, 110, 97, 109, 101, 130, 11, 101, 120, 97, 109, 112, 108, 101, 46, 99, 111, 109
                };
        const string subjectName1 = "not-real-subject-name";
        var x509Extension = new X509Extension(Oid, x509ExtensionBytes, true);
        var x509ExtensionFormattedString = x509Extension.Format(false);

        // Each OS has a different dNSName identifier and delimiter
        // On Windows, dNSName == "DNS Name" (localizable), on Linux, dNSName == "DNS"
        // e.g.,
        // Windows: x509ExtensionFormattedString is: "DNS Name=not-real-subject-name, DNS Name=example.com"
        // Linux:   x509ExtensionFormattedString is: "DNS:not-real-subject-name, DNS:example.com"
        // Parse: <identifier><delimiter><value><separator(s)>

        var delimiterIndex = x509ExtensionFormattedString.IndexOf(subjectName1, StringComparison.Ordinal) - 1;
        delimiter = x509ExtensionFormattedString[delimiterIndex];

        // Make an assumption that all characters from the the start of string to the delimiter
        // are part of the identifier
        identifier = x509ExtensionFormattedString.Substring(0, delimiterIndex);

        var separatorFirstChar = delimiterIndex + subjectName1.Length + 1;
        var separatorLength = 1;
        for (var i = separatorFirstChar + 1; i < x509ExtensionFormattedString.Length; i++)
        {
            // We advance until the first character of the identifier to determine what the
            // separator is. This assumes that the identifier assumption above is correct
            if (x509ExtensionFormattedString[i] == identifier[0])
            {
                break;
            }

            separatorLength++;
        }

        separator = x509ExtensionFormattedString.Substring(separatorFirstChar, separatorLength);
    }

    public static bool IsSelfSigned(this X509Certificate2 cert)
    {
        return cert.SubjectName.RawData.SequenceEqual(cert.IssuerName.RawData);
    }

    public static IEnumerable<string> GetAllDnsNames(this X509Certificate2 certificate)
    {
        yield return GetCommonName(certificate);
        foreach (var subjectAltName in GetDnsFromExtensions(certificate))
        {
            yield return subjectAltName;
        }
    }

    public static string GetCommonName(this X509Certificate2 certificate)
    {
        return certificate.GetNameInfo(X509NameType.SimpleName, false);
    }

    public static IEnumerable<string> GetDnsFromExtensions(this X509Certificate2 cert)
    {
        foreach (var ext in cert.Extensions)
        {
            // Extension is SAN2
            if (ext.Oid?.Value == Oid)
            {
                var asnString = ext.Format(false);
                if (string.IsNullOrWhiteSpace(asnString))
                {
                    yield break;
                }

                // SubjectAlternativeNames might contain something other than a dNSName,
                // so we have to parse through and only use the dNSNames
                // <identifier><delimiter><value><separator(s)>

                var rawDnsEntries =
                    asnString.Split(new string[1] { separator }, StringSplitOptions.RemoveEmptyEntries);

                var dnsEntries = new List<string>();

                for (var i = 0; i < rawDnsEntries.Length; i++)
                {
                    var keyval = rawDnsEntries[i].Split(delimiter);
                    if (string.Equals(keyval[0], identifier, StringComparison.Ordinal))
                    {
                        yield return keyval[1];
                    }
                }
            }
        }
    }

    public static string ExportPem(this X509Certificate2 cert)
    {
        var certificatePem = cert.ExportCertificatePem();

        if (cert.HasPrivateKey)
        {
            AsymmetricAlgorithm key = cert.GetRSAPrivateKey();
            key ??= cert.GetECDsaPrivateKey();
            key ??= cert.GetDSAPrivateKey();
            key ??= cert.GetECDiffieHellmanPrivateKey();
            if (key != null)
            {
                string pubKeyPem = key.ExportSubjectPublicKeyInfoPem();
                string privKeyPem = key.ExportPkcs8PrivateKeyPem();
                return $"{certificatePem}{Environment.NewLine}{pubKeyPem}{Environment.NewLine}{privKeyPem}";
            }
        }
        return certificatePem;
    }

    public static (string, string) ExportPemPair(this X509Certificate2 cert)
    {
        string text = cert.ExportCertificatePem();
        if (cert.HasPrivateKey)
        {
            AsymmetricAlgorithm asymmetricAlgorithm = cert.GetRSAPrivateKey();
            if (asymmetricAlgorithm == null)
            {
                asymmetricAlgorithm = cert.GetECDsaPrivateKey();
            }

            if (asymmetricAlgorithm == null)
            {
                asymmetricAlgorithm = cert.GetDSAPrivateKey();
            }

            if (asymmetricAlgorithm == null)
            {
                asymmetricAlgorithm = cert.GetECDiffieHellmanPrivateKey();
            }

            if (asymmetricAlgorithm != null)
            {
                string value = asymmetricAlgorithm.ExportSubjectPublicKeyInfoPem();
                string value2 = asymmetricAlgorithm.ExportPkcs8PrivateKeyPem();
                return (text, $"{value}{Environment.NewLine}{value2}");
            }
        }

        return (text, null);
    }
}