using Microsoft.AspNetCore.Http;
using VKProxy.Features;

namespace VKProxy.Middlewares.Http;

public interface IHttpReverseProxy
{
    Task Proxy(HttpContext context, IReverseProxyFeature proxyFeature);
}

public class HttpReverseProxy : IHttpReverseProxy
{
    public Task Proxy(HttpContext context, IReverseProxyFeature proxyFeature)
    {
        return Task.CompletedTask;
    }
}