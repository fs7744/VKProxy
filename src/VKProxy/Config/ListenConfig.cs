using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using VKProxy.Core.Config;

namespace VKProxy.Config;

public class ListenConfig : EndPointOptions
{
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
        return null;
    }
}