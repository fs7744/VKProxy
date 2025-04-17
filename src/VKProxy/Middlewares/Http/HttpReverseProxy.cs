using Microsoft.AspNetCore.Http;
using VKProxy.Features;

namespace VKProxy.Middlewares.Http;

public class HttpReverseProxy : IMiddleware
{
    private readonly IHttpSelector selector;

    public HttpReverseProxy(IHttpSelector selector)
    {
        this.selector = selector;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var resp = context.Response;
        if (resp.HasStarted) return;
        var proxyFeature = context.Features.Get<IReverseProxyFeature>();
        if (proxyFeature is not null)
        {
            var route = proxyFeature.Route;
            if (route is null)
            {
                route = proxyFeature.Route = await selector.MatchAsync(context);
            }
            if (route is not null)
            {
                resp.StatusCode = 204;
                await resp.CompleteAsync();
                return;
            }
        }
        //if (resp.HasStarted) return;
        resp.StatusCode = 404;
        await resp.CompleteAsync();
        return;
    }
}