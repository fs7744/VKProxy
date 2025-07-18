using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using VKProxy.ACME;
using VKProxy.ACME.Resource;

namespace UT.ACME.Crypto;

public class KeyInfoTests
{
    private static string accountKeyV1;

    public static string GetTestKey(KeyAlgorithm algo)
    {
        switch (algo)
        {
            case KeyAlgorithm.ES256:
                return Keys.ES256Key;

            case KeyAlgorithm.ES384:
                return Keys.ES384Key;

            case KeyAlgorithm.ES512:
                return Keys.ES512Key;

            default:
                return Keys.RS256Key;
        }
    }

    public static string GetTestKeyV1()
    {
        if (accountKeyV1 != null)
        {
            return accountKeyV1;
        }
        var pem = GetTestKey(KeyAlgorithm.RS256);
        using (var reader = new StringReader(pem))
        {
            var pemReader = new PemReader(reader);
            var pemKey = (AsymmetricCipherKeyPair)pemReader.ReadObject();
            var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pemKey.Private);
            return accountKeyV1 = Convert.ToBase64String(privateKey.GetDerEncoded());
        }
    }

    [Fact]
    public void CanReloadKeyPair()
    {
        var keyInfo = new KeyInfo
        {
            PrivateKeyInfo = Convert.FromBase64String(GetTestKeyV1())
        };

        var keyPair = keyInfo.CreateKeyPair();
        var exported = keyPair.Export();

        Assert.Equal(GetTestKeyV1(), Convert.ToBase64String(exported.PrivateKeyInfo));
    }

    [Fact]
    public void LoadKeyWithInvalidObject()
    {
        Assert.Throws<AcmeException>(() => KeyInfo.From(new MemoryStream()));
    }
}