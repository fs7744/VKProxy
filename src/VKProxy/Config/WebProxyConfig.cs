namespace VKProxy.Config;

public class WebProxyConfig
{/// <summary>
 /// The URI of the proxy server.
 /// </summary>
    public Uri? Address { get; set; }

    /// <summary>
    /// true to bypass the proxy for local addresses; otherwise, false.
    /// If null, default value will be used: false
    /// </summary>
    public bool? BypassOnLocal { get; set; }

    /// <summary>
    /// Controls whether the <seealso cref="System.Net.CredentialCache.DefaultCredentials"/> are sent with requests.
    /// If null, default value will be used: false
    /// </summary>
    public bool? UseDefaultCredentials { get; set; }

    public static bool Equals(WebProxyConfig? t, WebProxyConfig? other)
    {
        if (other is null)
        {
            return t is null;
        }

        if (t is null)
        {
            return other is null;
        }

        return t.Address == other.Address
            && t.BypassOnLocal == other.BypassOnLocal
            && t.UseDefaultCredentials == other.UseDefaultCredentials;
    }

    public override bool Equals(object? obj)
    {
        return obj is WebProxyConfig o && Equals(this, o);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Address,
            BypassOnLocal,
            UseDefaultCredentials
        );
    }
}