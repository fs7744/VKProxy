namespace VKProxy.ACME.Crypto;

public interface IKeyAlgorithm
{
    ISigner CreateSigner(Key key);

    Key GenerateKey(int? keySize = null);
}