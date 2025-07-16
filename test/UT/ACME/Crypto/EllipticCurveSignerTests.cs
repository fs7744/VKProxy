using System.Security.Cryptography;
using System.Text;
using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace UT.ACME.Crypto;

public class EllipticCurveSignerTests
{
    [Fact]
    public void InvalidPrivateKey()
    {
        var algo = KeyAlgorithmProvider.Get(KeyAlgorithm.RS256);
        var key = algo.GenerateKey();

        Assert.Throws<ArgumentException>(() => new EllipticCurveSigner(key, "algo", "algo"));
    }

    [Theory]
    [InlineData(KeyAlgorithm.RS256)]
    [InlineData(KeyAlgorithm.ES256)]
    public void CanComputeHash(KeyAlgorithm algoType)
    {
        var algo = KeyAlgorithmProvider.Get(algoType);
        var signer = algo.CreateSigner(algo.GenerateKey());

        var data = Encoding.UTF8.GetBytes("secret message");
        var hash = signer.ComputeHash(data);

        using (var sha = SHA256.Create())
        {
            Assert.Equal(sha.ComputeHash(data), hash);
        }
    }
}