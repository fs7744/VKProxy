using VKProxy.Core.Sockets.Udp;
using VKProxy.Features;

namespace VKProxy.Middlewares;

internal interface IUdpReverseProxy
{
    Task Proxy(UdpConnectionContext context, IReverseProxyFeature feature);
}