namespace VKProxy.ACME.Resource;

public class Challenge
{
    public string Type { get; set; }
    public Uri Url { get; set; }
    public ChallengeStatus? Status { get; set; }
    public DateTimeOffset? Validated { get; set; }
    public AcmeError Error { get; set; }
    public string Token { get; set; }

    /// <summary>
    /// The http-01 challenge.
    /// </summary>
    public const string Http01 = "http-01";

    /// <summary>
    /// The dns-01 challenge.
    /// </summary>
    public const string Dns01 = "dns-01";

    /// <summary>
    /// Gets the tls-alpn-01 challenge name.
    /// </summary>
    /// <value>
    /// The tls-alpn-01 challenge name.
    /// </value>
    public const string TlsAlpn01 = "tls-alpn-01";
}