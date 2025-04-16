using Microsoft.AspNetCore.Http;
using VKProxy.Features;

namespace VKProxy.Middlewares.Http;

public interface IHttpReverseProxy
{
    Task Proxy(HttpContext context, IReverseProxyFeature proxyFeature);
}

public class HttpReverseProxy : IHttpReverseProxy
{
    private readonly IHttpSelector selector;

    public HttpReverseProxy(IHttpSelector selector)
    {
        this.selector = selector;
    }

    public async Task Proxy(HttpContext context, IReverseProxyFeature proxyFeature)
    {
        var resp = context.Response;
        if (resp.HasStarted) return;
        var route = proxyFeature.Route;
        if (route is null)
        {
            route = proxyFeature.Route = await selector.MatchAsync(context);
        }
        if (route is null)
        {
            resp.StatusCode = 404;
            await resp.CompleteAsync();
            return;
        }
    }
}