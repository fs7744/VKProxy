namespace VKProxy.ACME.Crypto;

public interface IKey : IEncodable
{
    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <value>
    /// The algorithm.
    /// </value>
    KeyAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the json web key.
    /// </summary>
    /// <value>
    /// The json web key.
    /// </value>
    JsonWebKey JsonWebKey { get; }
}