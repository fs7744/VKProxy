using System.Text.Json.Serialization;

namespace VKProxy.ACME.Crypto;

/// <summary>
/// Represents and JSON web key.
/// Note that inheriting classes must define JSON serialisation order to maintain lexographic order as per https://tools.ietf.org/html/rfc7638#page-8
/// </summary>
public class JsonWebKey
{
    /// <summary>
    /// Gets or sets the type of the key.
    /// </summary>
    /// <value>
    /// The type of the key.
    /// </value>
    [JsonPropertyName("kty")]
    [JsonPropertyOrder(2)]
    public string KeyType { get; set; }
}

public class EcJsonWebKey : JsonWebKey
{
    /// <summary>
    /// Gets or sets the curve identifies the cryptographic curve used with the key.
    /// </summary>
    /// <value>
    /// The curve identifies the cryptographic curve used with the key.
    /// </value>
    [JsonPropertyName("crv")]
    [JsonPropertyOrder(1)]
    public string Curve { get; set; }

    /// <summary>
    /// Gets or sets the x coordinate for the Elliptic Curve point.
    /// </summary>
    /// <value>
    /// The x coordinate for the Elliptic Curve point.
    /// </value>
    [JsonPropertyName("x")]
    [JsonPropertyOrder(3)]
    public string X { get; set; }

    /// <summary>
    /// Gets or sets the y coordinate for the Elliptic Curve point.
    /// </summary>
    /// <value>
    /// The y coordinate for the Elliptic Curve point.
    /// </value>
    [JsonPropertyName("y")]
    [JsonPropertyOrder(4)]
    public string Y { get; set; }
}

public class RsaJsonWebKey : JsonWebKey
{
    /// <summary>
    /// Gets or sets the exponent value for the RSA public key.
    /// </summary>
    /// <value>
    /// The exponent value for the RSA public key.
    /// </value>
    [JsonPropertyName("e")]
    [JsonPropertyOrder(1)]
    public string Exponent { get; set; }

    /// <summary>
    /// Gets or sets the modulus value for the RSA public key.
    /// </summary>
    /// <value>
    /// The modulus value for the RSA public key.
    /// </value>
    [JsonPropertyName("n")]
    [JsonPropertyOrder(3)]
    public string Modulus { get; set; }
}