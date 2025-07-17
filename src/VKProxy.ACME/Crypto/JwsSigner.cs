using System.Text;
using System.Text.Json;

namespace VKProxy.ACME.Crypto;

/// <summary>
/// Represents data signed with JWS.
/// </summary>
public class JwsPayload
{
    public string Protected { get; set; }
    public string Payload { get; set; }
    public string Signature { get; set; }
}

/// <summary>
/// Represents an signer for JSON Web Signature.
/// </summary>
public class JwsSigner
{
    private readonly Key keyPair;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwsSigner"/> class.
    /// </summary>
    /// <param name="keyPair">The keyPair.</param>
    public JwsSigner(Key keyPair)
    {
        this.keyPair = keyPair;
    }

    /// <summary>
    /// Signs the specified payload.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <param name="nonce">The nonce.</param>
    /// <returns>The signed payload.</returns>
    public JwsPayload Sign(object payload, string nonce)
        => Sign(payload, null, null, nonce);

    /// <summary>
    /// Encodes this instance.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="url">The URL.</param>
    /// <param name="nonce">The nonce.</param>
    /// <returns>The signed payload.</returns>
    public JwsPayload Sign(
        object payload,
        Uri keyId = null,
        Uri url = null,
        string nonce = null)
    {
        var protectedHeader = (keyId) == null ?
            (object)new
            {
                alg = keyPair.Algorithm.ToJwsAlgorithm(),
                jwk = keyPair.JsonWebKey,
                nonce,
                url,
            } :
            new
            {
                alg = keyPair.Algorithm.ToJwsAlgorithm(),
                kid = keyId,
                nonce,
                url,
            };

        var entityJson = payload == null ?
            "" :
            JsonSerializer.Serialize(payload, DefaultAcmeHttpClient.JsonSerializerOptions);
        var protectedHeaderJson = JsonSerializer.Serialize(protectedHeader, DefaultAcmeHttpClient.JsonSerializerOptions);

        var payloadEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(entityJson));
        var protectedHeaderEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(protectedHeaderJson));

        var signature = $"{protectedHeaderEncoded}.{payloadEncoded}";
        var signatureBytes = Encoding.UTF8.GetBytes(signature);
        var signedSignatureBytes = keyPair.GetSigner().SignData(signatureBytes);
        var signedSignatureEncoded = JwsConvert.ToBase64String(signedSignatureBytes);

        var body = new JwsPayload
        {
            Protected = protectedHeaderEncoded,
            Payload = payloadEncoded,
            Signature = signedSignatureEncoded
        };

        return body;
    }
}