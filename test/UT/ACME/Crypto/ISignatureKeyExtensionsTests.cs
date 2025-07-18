using System.Security.Cryptography;
using System.Text;
using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace UT.ACME.Crypto;

public class ISignatureKeyExtensionsTests
{
    [Fact]
    public void CanGenerateDnsRecordValue()
    {
        var key = KeyAlgorithm.ES256.NewKey();
        using (var sha256 = SHA256.Create())
        {
            Assert.Equal(
                JwsConvert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(key.KeyAuthorization("token")))),
                key.DnsTxt("token"));
        }
    }
}