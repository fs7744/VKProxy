using VKProxy.Core.Sockets.Udp;
using VKProxy.Features;

namespace VKProxy.Middlewares;

public interface IUdpReverseProxy
{
    Task Proxy(UdpConnectionContext context, IL4ReverseProxyFeature feature);
}