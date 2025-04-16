using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using VKProxy.Core.Config;

namespace VKProxy.Config;

public class ListenConfig
{
    public string Key { get; set; }

    public GatewayProtocols Protocols { get; set; }

    public string[]? Address { get; set; }

    public bool UseSni { get; set; }
    public string? SniId { get; set; }

    public string? RouteId { get; set; }

    internal SniConfig? SniConfig { get; set; }

    internal RouteConfig? RouteConfig { get; set; }

    internal List<ListenEndPointOptions> ListenEndPointOptions { get; set; }
    private HttpsConnectionAdapterOptions httpsConnectionAdapterOptions;

    internal HttpsConnectionAdapterOptions GetHttpsOptions()
    {
        if (UseSni)
        {
            if (httpsConnectionAdapterOptions is null)
            {
                httpsConnectionAdapterOptions = SniConfig?.GenerateHttps();
                return httpsConnectionAdapterOptions;
            }
        }
        return null;
    }
}

public class ListenEndPointOptions : EndPointOptions
{
    internal ListenConfig Parent { get; set; }

    public GatewayProtocols Protocols { get; set; }
    public string SniId => Parent?.SniId;

    public string RouteId => Parent?.RouteId;

    internal SniConfig? SniConfig => Parent?.SniConfig;
    internal RouteConfig? RouteConfig => Parent?.RouteConfig;

    public bool UseSni { get; set; }

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