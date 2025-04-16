using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using VKProxy.Config;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Features;
using VKProxy.LoadBalancing;

namespace VKProxy.Middlewares;

internal interface ITcpReverseProxy
{
    Task Proxy(ConnectionContext context, IReverseProxyFeature feature);
}

internal class TcpReverseProxy : ITcpReverseProxy
{
    private readonly IConnectionFactory tcp;
    private readonly ProxyLogger logger;
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    private readonly ReverseProxyOptions options;
    private readonly TcpProxyDelegate req;
    private readonly TcpProxyDelegate resp;
    private readonly TcpDelegate init;

    public TcpReverseProxy(IConnectionFactory tcp, ProxyLogger logger, ILoadBalancingPolicyFactory loadBalancing, IEnumerable<ITcpProxyMiddleware> middlewares, IOptions<ReverseProxyOptions> options)
    {
        this.tcp = tcp;
        this.logger = logger;
        this.loadBalancing = loadBalancing;
        this.options = options.Value;
        (init, req, resp) = BuildMiddlewares(middlewares);
    }

    private (TcpDelegate init, TcpProxyDelegate req, TcpProxyDelegate resp) BuildMiddlewares(IEnumerable<ITcpProxyMiddleware> middlewares)
    {
        TcpDelegate init = (context, c) => Task.CompletedTask;
        TcpProxyDelegate req = (context, s, c) => Task.FromResult(s);
        TcpProxyDelegate resp = (context, s, c) => Task.FromResult(s);

        foreach (var middleware in middlewares)
        {
            Func<TcpDelegate, TcpDelegate> m = (next) => (c, t) => middleware.InitAsync(c, t, next);
            init = m(init);

            Func<TcpProxyDelegate, TcpProxyDelegate> r = (next) => (c, s, t) => middleware.OnRequestAsync(c, s, t, next);
            req = r(req);

            r = (next) => (c, s, t) => middleware.OnResponseAsync(c, s, t, next);
            resp = r(resp);
        }

        return (init, req, resp);
    }

    public async Task Proxy(ConnectionContext context, IReverseProxyFeature feature)
    {
        var route = feature.Route;
        if (route is null) return;
        ConnectionContext upstream = null;
        try
        {
            using var cts = CancellationTokenSourcePool.Default.Rent(route.Timeout);
            var token = cts.Token;
            await init(context, token);
            if (feature.IsDone) return;
            upstream = await DoConnectionAsync(feature, route, route.RetryCount);
            if (upstream is null)
            {
                logger.NotFoundAvailableUpstream(route.ClusterId);
            }
            else
            {
                feature.SelectedDestination?.ConcurrencyCounter.Increment();
                var task = await Task.WhenAny(
                        context.Transport.Input.CopyToAsync(new MiddlewarePipeWriter(upstream.Transport.Output, context, req), token)
                        , upstream.Transport.Input.CopyToAsync(new MiddlewarePipeWriter(context.Transport.Output, context, resp), token));
                if (task.IsCanceled)
                {
                    logger.ProxyTimeout(route.Key, route.Timeout);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.ConnectUpstreamTimeout(route.Key);
        }
        catch (Exception ex)
        {
            logger.UnexpectedException(nameof(TcpReverseProxy), ex);
        }
        finally
        {
            feature.SelectedDestination?.ConcurrencyCounter.Decrement();
            upstream?.Abort();
        }
    }

    private async Task<ConnectionContext> DoConnectionAsync(IReverseProxyFeature feature, RouteConfig route, int retryCount)
    {
        DestinationState selectedDestination = null;
        try
        {
            selectedDestination = loadBalancing.PickDestination(feature);
            if (selectedDestination is null)
            {
                return null;
            }
            using var cts = CancellationTokenSourcePool.Default.Rent(options.ConnectionTimeout);
            var c = await tcp.ConnectAsync(selectedDestination.EndPoint, cts.Token);
            selectedDestination.ReportSuccessed();
            return c;
        }
        catch
        {
            selectedDestination?.ReportFailed();
            retryCount--;
            if (retryCount < 0)
            {
                throw;
            }
            return await DoConnectionAsync(feature, route, retryCount);
        }
    }
}