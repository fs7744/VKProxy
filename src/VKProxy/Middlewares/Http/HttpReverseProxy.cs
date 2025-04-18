using Microsoft.AspNetCore.Http;
using VKProxy.Config;
using VKProxy.Core.Loggers;
using VKProxy.Features;
using VKProxy.LoadBalancing;

namespace VKProxy.Middlewares.Http;

public class HttpReverseProxy : IMiddleware
{
    private readonly IHttpSelector selector;
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    private readonly ProxyLogger logger;
    private readonly IHttpForwarder forwarder;

    public HttpReverseProxy(IHttpSelector selector, ILoadBalancingPolicyFactory loadBalancing, ProxyLogger logger, IHttpForwarder forwarder)
    {
        this.selector = selector;
        this.loadBalancing = loadBalancing;
        this.logger = logger;
        this.forwarder = forwarder;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var resp = context.Response;
        if (resp.HasStarted) return;
        var proxyFeature = context.Features.Get<IReverseProxyFeature>();
        if (proxyFeature is not null)
        {
            var route = proxyFeature.Route;
            route ??= proxyFeature.Route = await selector.MatchAsync(context);
            if (route is not null)
            {
                var cluster = route.ClusterConfig;
                DestinationState selectedDestination;
                if (cluster is null)
                {
                    selectedDestination = null;
                }
                else
                {
                    selectedDestination = proxyFeature.SelectedDestination;
                    selectedDestination ??= loadBalancing.PickDestination(proxyFeature);
                }

                if (selectedDestination is null)
                {
                    logger.NotFoundAvailableUpstream(route.ClusterId);
                    resp.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return;
                }
                var result = await forwarder.SendAsync(context, selectedDestination, cluster, route.Transformer);
                return;
            }
        }

        resp.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
}