using Org.BouncyCastle.X509;
using System.Text;
using VKProxy.ACME.Crypto;

namespace VKProxy.ACME.Resource;

public class CertificateChain
{
    public CertificateChain(string certificateChain)
    {
        var certificates = certificateChain
            .Split(new[] { "-----END CERTIFICATE-----" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c + "-----END CERTIFICATE-----");

        Certificate = new CertificateContent(certificates.First());
        Issuers = certificates.Skip(1).Select(c => new CertificateContent(c)).ToArray();
    }

    public IEncodable Certificate { get; }
    public IList<IEncodable> Issuers { get; }

    /// <summary>
    /// Checks if the certificate chain is signed by a preferred issuer.
    /// </summary>
    /// <param name="preferredChain">The name of the preferred issuer</param>
    /// <returns>true if a certificate in the chain is issued by preferredChain or preferredChain is empty</returns>
    public bool MatchesPreferredChain(string preferredChain)
    {
        if (string.IsNullOrEmpty(preferredChain))
            return true;

        var certParser = new X509CertificateParser();
        var allcerts = Issuers.Select(x => x.ToPem()).ToList();
        allcerts.Insert(0, Certificate.ToPem());
        foreach (var pem in allcerts)
        {
            var cert = certParser.ReadCertificate(Encoding.UTF8.GetBytes(pem));
            if (cert.IssuerDN.GetValueList().Contains(preferredChain))
                return true;
        }

        return false;
    }
}

internal class CertificateContent : IEncodable
{
    private readonly string pem;

    public CertificateContent(string pem)
    {
        this.pem = pem.Trim();
    }

    public byte[] ToDer()
    {
        var certParser = new X509CertificateParser();
        var cert = certParser.ReadCertificate(
            Encoding.UTF8.GetBytes(pem));
        return cert.GetEncoded();
    }

    public string ToPem() => pem;
}