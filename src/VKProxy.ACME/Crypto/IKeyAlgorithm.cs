namespace VKProxy.ACME.Crypto;

public interface IKeyAlgorithm
{
    ISigner CreateSigner(IKey key);

    IKey GenerateKey(int? keySize = null);
}