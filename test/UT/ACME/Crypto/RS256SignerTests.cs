using Moq;
using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace UT.ACME.Crypto;

public class RS256SignerTests
{
    [Fact]
    public void InvalidPrivateKey()
    {
        var algo = KeyAlgorithmProvider.Get(KeyAlgorithm.ES256);
        var key = algo.GenerateKey();

        Assert.Throws<ArgumentException>(() => new RS256Signer(key));
    }

    [Fact]
    public void InvalidKey()
    {
        var mock = new Mock<IKey>();
        var obj = mock.Object;
        Assert.Throws<ArgumentException>(() => new RS256Signer(obj));
    }
}