using VKProxy.ACME.Resource;

namespace UT.ACME;

public class ResourceTest
{
    [Fact]
    public void Directory()
    {
        var data = """
            {
              "_gYvNb0fx54": "https://community.letsencrypt.org/t/adding-random-entries-to-the-directory/33417",
              "keyChange": "https://acme-staging-v02.api.letsencrypt.org/acme/key-change",
              "meta": {
                "caaIdentities": [
                  "letsencrypt.org"
                ],
                "profiles": {
                  "classic": "https://letsencrypt.org/docs/profiles#classic",
                  "shortlived": "https://letsencrypt.org/docs/profiles#shortlived (not yet generally available)",
                  "tlsserver": "https://letsencrypt.org/docs/profiles#tlsserver"
                },
                "termsOfService": "https://letsencrypt.org/documents/LE-SA-v1.5-February-24-2025.pdf",
                "website": "https://letsencrypt.org/docs/staging-environment/"
              },
              "newAccount": "https://acme-staging-v02.api.letsencrypt.org/acme/new-acct",
              "newNonce": "https://acme-staging-v02.api.letsencrypt.org/acme/new-nonce",
              "newOrder": "https://acme-staging-v02.api.letsencrypt.org/acme/new-order",
              "renewalInfo": "https://acme-staging-v02.api.letsencrypt.org/acme/renewal-info",
              "revokeCert": "https://acme-staging-v02.api.letsencrypt.org/acme/revoke-cert"
            }
            """;
        var d = System.Text.Json.JsonSerializer.Deserialize<AcmeDirectory>(data, new System.Text.Json.JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        Assert.NotNull(d.RenewalInfo);
    }
}