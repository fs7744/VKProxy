using VKProxy.ACME.Crypto;

namespace VKProxy.ACME;

public abstract class Key : IEncodable
{
    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <value>
    /// The algorithm.
    /// </value>
    public abstract KeyAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the json web key.
    /// </summary>
    /// <value>
    /// The json web key.
    /// </value>
    public abstract JsonWebKey JsonWebKey { get; }

    public abstract byte[] ToDer();

    public abstract string ToPem();

    public static implicit operator Key(string pem) => KeyAlgorithmProvider.GetKey(pem);

    public static implicit operator Key(byte[] der) => KeyAlgorithmProvider.GetKey(der);
}