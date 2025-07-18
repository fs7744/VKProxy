using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using System.Text.Json.Serialization;

namespace VKProxy.ACME.Resource;

/// <summary>
/// Represents a key pair.
/// </summary>
public class KeyInfo
{
    /// <summary>
    /// Gets or sets the private key information.
    /// </summary>
    /// <value>
    /// The private key information.
    /// </value>
    [JsonPropertyName("der")]
    public byte[] PrivateKeyInfo { get; set; }

    /// <summary>
    /// Reads the key from the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The steam.</param>
    /// <returns>The key loaded.</returns>
    public static KeyInfo From(Stream stream)
    {
        using (var streamReader = new StreamReader(stream))
        {
            var reader = new PemReader(streamReader);

            if (!(reader.ReadObject() is AsymmetricCipherKeyPair keyPair))
            {
                throw new AcmeException("Invaid key data.");
            }

            return keyPair.Export();
        }
    }
}