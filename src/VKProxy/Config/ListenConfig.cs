using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Authentication;
using VKProxy.Core.Config;

namespace VKProxy.Config;

public class ListenConfig
{
    public string Key { get; set; }

    public GatewayProtocols Protocols { get; set; }

    public string[]? Address { get; set; }

    public bool UseSni { get; set; }

    public TimeSpan? HandshakeTimeout { get; set; }

    public SslProtocols? TlsProtocols { get; set; }
    public bool? CheckCertificateRevocation { get; set; }
    public ClientCertificateMode? ClientCertificateMode { get; set; }

    internal List<ListenEndPointOptions> ListenEndPointOptions { get; set; }
    private HttpsConnectionAdapterOptions httpsConnectionAdapterOptions;

    internal HttpsConnectionAdapterOptions GetHttpsOptions()
    {
        if (UseSni)
        {
            if (httpsConnectionAdapterOptions is null)
            {
                httpsConnectionAdapterOptions = GenerateHttps();
                return httpsConnectionAdapterOptions;
            }
        }
        return null;
    }

    private HttpsConnectionAdapterOptions? GenerateHttps()
    {
        var s = new HttpsConnectionAdapterOptions();
        if (HandshakeTimeout.HasValue)
        {
            s.HandshakeTimeout = HandshakeTimeout.Value;
        }
        if (TlsProtocols.HasValue)
        {
            s.SslProtocols = TlsProtocols.Value;
        }
        if (CheckCertificateRevocation.HasValue)
        {
            s.CheckCertificateRevocation = CheckCertificateRevocation.Value;
        }
        if (ClientCertificateMode.HasValue)
        {
            s.ClientCertificateMode = ClientCertificateMode.Value;
        }
        return s;
    }
}

public class ListenEndPointOptions : EndPointOptions
{
    internal ListenConfig Parent { get; set; }

    public GatewayProtocols Protocols { get; set; }

    internal HttpProtocols GetHttpProtocols()
    {
        var r = Protocols.HasFlag(GatewayProtocols.HTTP1) ? HttpProtocols.Http1 : HttpProtocols.None;
        if (Protocols.HasFlag(GatewayProtocols.HTTP2))
        {
            r |= HttpProtocols.Http2;
        }
        if (Protocols.HasFlag(GatewayProtocols.HTTP3))
        {
            r |= HttpProtocols.Http3;
        }
        return r;
    }

    internal HttpsConnectionAdapterOptions GetHttpsOptions()
    {
        return Parent?.GetHttpsOptions();
    }

    public override string ToString()
    {
        return $"[Key: {Key},Protocols: {Protocols},EndPoint: {EndPoint}]";
    }
}