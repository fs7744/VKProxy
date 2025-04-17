using System.Security.Authentication;

namespace VKProxy.Config;

public sealed class HttpClientConfig
{
    /// <summary>
    /// What TLS protocols to use.
    /// </summary>
    public SslProtocols? SslProtocols { get; init; }

    /// <summary>
    /// Indicates if destination server https certificate errors should be ignored.
    /// This should only be done when using self-signed certificates.
    /// </summary>
    public bool? DangerousAcceptAnyServerCertificate { get; init; }

    /// <summary>
    /// Limits the number of connections used when communicating with the destination server.
    /// </summary>
    public int? MaxConnectionsPerServer { get; init; }

    /// <summary>
    /// Optional web proxy used when communicating with the destination server.
    /// </summary>
    public WebProxyConfig? WebProxy { get; init; }

    /// <summary>
    /// Gets or sets a value that indicates whether additional HTTP/2 connections can
    /// be established to the same server when the maximum number of concurrent streams
    /// is reached on all existing connections.
    /// </summary>
    public bool? EnableMultipleHttp2Connections { get; init; }

    /// <summary>
    /// Allows overriding the default (ASCII) encoding for outgoing request headers.
    /// <para>
    /// Setting this value will in turn set <see cref="SocketsHttpHandler.RequestHeaderEncodingSelector"/> and use the selected encoding for all request headers.
    /// The value is then parsed by <see cref="Encoding.GetEncoding(string)"/>, so use values like: "utf-8", "iso-8859-1", etc.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note: If you're using an encoding other than UTF-8 here, then you may also need to configure your server to accept request headers with such an encoding via the corresponding options for the server.
    /// </remarks>
    public string? RequestHeaderEncoding { get; init; }

    /// <summary>
    /// Allows overriding the default (Latin1) encoding for incoming request headers.
    /// <para>
    /// Setting this value will in turn set <see cref="SocketsHttpHandler.ResponseHeaderEncodingSelector"/> and use the selected encoding for all response headers.
    /// The value is then parsed by <see cref="Encoding.GetEncoding(string)"/>, so use values like: "utf-8", "iso-8859-1", etc.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note: If you're using an encoding other than ASCII here, then you may also need to configure your server to send response headers with such an encoding via the corresponding options for the server.
    /// </remarks>
    public string? ResponseHeaderEncoding { get; init; }

    public static bool Equals(HttpClientConfig? t, HttpClientConfig? other)
    {
        if (other is null)
        {
            return t is null;
        }

        if (t is null)
        {
            return other is null;
        }

        return t.SslProtocols == other.SslProtocols
               && t.DangerousAcceptAnyServerCertificate == other.DangerousAcceptAnyServerCertificate
               && t.MaxConnectionsPerServer == other.MaxConnectionsPerServer
               && t.EnableMultipleHttp2Connections == other.EnableMultipleHttp2Connections
               && t.RequestHeaderEncoding == other.RequestHeaderEncoding
               && t.ResponseHeaderEncoding == other.ResponseHeaderEncoding
               && WebProxyConfig.Equals(t.WebProxy, other.WebProxy);
    }

    public override bool Equals(object? obj)
    {
        return obj is HttpClientConfig o && Equals(this, o);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SslProtocols,
            DangerousAcceptAnyServerCertificate,
            MaxConnectionsPerServer,
            EnableMultipleHttp2Connections,
            RequestHeaderEncoding,
            ResponseHeaderEncoding,
            WebProxy);
    }
}