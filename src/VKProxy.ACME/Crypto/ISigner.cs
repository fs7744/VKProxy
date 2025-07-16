namespace VKProxy.ACME.Crypto;

public interface ISigner
{
    byte[] ComputeHash(byte[] data);

    byte[] SignData(byte[] data);
}