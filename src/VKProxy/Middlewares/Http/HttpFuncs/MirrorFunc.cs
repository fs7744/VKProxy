using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VKProxy.Config;
using VKProxy.Core.Infrastructure.Buffers;
using VKProxy.Core.Loggers;
using VKProxy.Features;
using VKProxy.LoadBalancing;
using VKProxy.Middlewares.Http.Transforms;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class MirrorFunc : IHttpFunc
{
    private readonly IServiceProvider serviceProvider;
    private readonly IHttpForwarder forwarder;
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    private readonly IForwarderHttpClientFactory forwarderHttpClientFactory;
    private readonly ProxyLogger logger;

    public int Order => int.MinValue;

    public MirrorFunc(IServiceProvider serviceProvider, IHttpForwarder forwarder, ILoadBalancingPolicyFactory loadBalancing, IForwarderHttpClientFactory forwarderHttpClientFactory, ProxyLogger logger)
    {
        this.serviceProvider = serviceProvider;
        this.forwarder = forwarder;
        this.loadBalancing = loadBalancing;
        this.forwarderHttpClientFactory = forwarderHttpClientFactory;
        this.logger = logger;
    }

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        if (config.Metadata == null || !config.Metadata.TryGetValue("MirrorCluster", out var mirrorCluster) || string.IsNullOrWhiteSpace(mirrorCluster)) return next;

        return c => Mirror(c, mirrorCluster, next);
    }

    private async Task Mirror(HttpContext c, string mirrorCluster, RequestDelegate next)
    {
        var config = serviceProvider.GetRequiredService<IConfigSource<IProxyConfig>>();
        if (config.CurrentSnapshot == null || config.CurrentSnapshot.Clusters == null || !config.CurrentSnapshot.Clusters.TryGetValue(mirrorCluster, out var cluster) || cluster == null)
        {
            await next(c);
            return;
        }

        c.Request.EnableBuffering();
        //var originBody = c.Request.Body;
        //using var buffer = new ReadBufferingStream(originBody);
        //c.Request.Body = buffer;

        try
        {
            await next(c);
        }
        finally
        {
            //c.Request.Body = buffer.BufferingStream;
            try
            {
                var proxyFeature = c.Features.GetRequiredFeature<IReverseProxyFeature>();
                var selectedDestination = loadBalancing.PickDestination(proxyFeature, cluster);
                if (selectedDestination != null)
                {
                    cluster.InitHttp(forwarderHttpClientFactory);
                    c.Request.Body.Seek(0, SeekOrigin.Begin);
                    await forwarder.SendAsync(c, proxyFeature, selectedDestination, cluster, new NonHttpTransformer(proxyFeature.Route.Transformer));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mirror failed");
            }
            //finally
            //{
            //    c.Request.Body = originBody;
            //}
        }
    }
}