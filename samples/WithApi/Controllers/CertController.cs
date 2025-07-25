using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using VKProxy.ACME.AspNetCore;
using VKProxy.Core.Extensions;

namespace WithApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CertController : ControllerBase
{
    private readonly IAcmeStateIniter initer;

    public CertController(IAcmeStateIniter initer)
    {
        this.initer = initer;
    }

    [HttpGet]
    public async Task<string> Get([FromQuery] string domain)
    {
        var o = new AcmeChallengeOptions()
        {
            AllowedChallengeTypes = VKProxy.ACME.AspNetCore.ChallengeType.Http01,
            RenewDaysInAdvance = TimeSpan.FromDays(2),
            Server = new Uri("https://127.0.0.1:14000/dir"),
            DomainNames = new[] { domain },
            AdditionalIssuers = new[] { """
                        -----BEGIN CERTIFICATE-----
                        MIIDGzCCAgOgAwIBAgIIUPFry5qBu34wDQYJKoZIhvcNAQELBQAwIDEeMBwGA1UE
                        AxMVUGViYmxlIFJvb3QgQ0EgMjFjNjY3MCAXDTI1MDcyMjAxMTA0OVoYDzIwNTUw
                        NzIyMDExMDQ5WjAgMR4wHAYDVQQDExVQZWJibGUgUm9vdCBDQSAyMWM2NjcwggEi
                        MA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCxNKa4y93OFYaSx8bcbuWsHHnW
                        mpfsobK5Elf7GE02mi/cDrMP+wR1l53BuucrW04OyoewkBsJNZoxEy1DkCjxv4+g
                        Q+HgGCR5R14ex17ZdFxpcl42H8QnRB3IqVBlJiz0JyGZwiaOamOkUTVEYTGDeuxu
                        PglpvboGeatsWQe0MJJfBN8OxLVUmi6Y/enbzlIdv3tvgQujfPNiS8MLDMBuIiMs
                        ixhu8YAzUqvVKZoQVK7GwbD9WrVBKub8w86StKFmU14aSXahidt8IENdpLO2OT3J
                        y1nt25QDsAmtS1/wGnTDPeefLGsM7kGYNesQkSW0w8Um4p9KLWKnKyOvzPZrAgMB
                        AAGjVzBVMA4GA1UdDwEB/wQEAwIChDATBgNVHSUEDDAKBggrBgEFBQcDATAPBgNV
                        HRMBAf8EBTADAQH/MB0GA1UdDgQWBBRoXcwo6c5J8jMweiHKPw4OlcWIQzANBgkq
                        hkiG9w0BAQsFAAOCAQEAad9XT4sN1KserYtCxBKmoPhPAHInHYgG/Z2gd6KqdsK9
                        biIgEbKo84tClLqA6XCN/yN1bMQL2ZMbWBF8oHv/A5o0atpTpd+Ho+punHYRIpqv
                        akUX21Zsu6NdAuH7g7m9t9h/lc6tgiqaAf2HwpC3NrXmUlPRqLay7/t+BFQU6dBa
                        E+qzmL7lHZQf1UArfb+QDYH2XsFCk9Pjv0xdP+PGwf8HqHhfPLctvus5JL+LXp0X
                        68eWKQCs1CrL8cUMwcELlW/mR1lKnJL1WgM1Bns9ZF1ha6egG539ruzQjItF6MHB
                        xAEt55nXfs+mjV1p7qrcmR8jIdByR9C36T21r+8pKA==
                        -----END CERTIFICATE-----

                        """
            }
        };
        o.NewAccount(new string[] { "mailto:test11@xxx.com" });
        var cert = await initer.CreateCertificateAsync(o);
        return cert.ExportPem();
    }
}