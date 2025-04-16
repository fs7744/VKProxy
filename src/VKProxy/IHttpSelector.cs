using DotNext;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VKProxy.Config;
using VKProxy.Core.Loggers;
using VKProxy.Core.Routing;

namespace VKProxy;

public interface IHttpSelector
{
    ValueTask<RouteConfig> MatchAsync(HttpContext context);

    RouteConfig Match(HttpContext context);

    Task ReBuildAsync(IReadOnlyDictionary<string, RouteConfig> routes, CancellationToken cancellationToken);
}

public class PathSelector : IRouteData<RouteConfig>
{
    private List<RouteConfig> RouteConfigs = new List<RouteConfig>();
    private IRouteTable<RouteConfig> route;

    public void Add(RouteConfig value)
    {
        RouteConfigs.Add(value);
    }

    public void Add(IRouteData<RouteConfig> value)
    {
        RouteConfigs.AddRange(((PathSelector)value).RouteConfigs);
    }

    public void Init(int cacheSize)
    {
        var builder = new RouteTableBuilder<RouteConfig>(StringComparison.OrdinalIgnoreCase, cacheSize);
        foreach (var route in RouteConfigs.Where(i => i.Match != null && i.Match.Paths != null))
        {
            foreach (var path in route.Match.Paths)
            {
                if (path.EndsWith('*'))
                {
                    builder.Add(path[..^1], route, RouteType.Prefix, route.Order);
                }
                else
                {
                    builder.Add(path, route, RouteType.Exact, route.Order);
                }
            }
        }

        route = builder.Build(RouteTableType.Complex);
        RouteConfigs = null;
    }

    public RouteConfig Match<R2>(string key, R2 data, Func<RouteConfig, R2, bool> match)
    {
        return route.Match(key, data, match);
    }

    public ValueTask<RouteConfig> MatchAsync<R2>(string key, R2 data, Func<RouteConfig, R2, bool> match)
    {
        return route.MatchAsync(key, data, match);
    }
}

public class HttpSelector : IHttpSelector
{
    private readonly ReverseProxyOptions options;
    private readonly ProxyLogger logger;
    private RouteTable<RouteConfig, PathSelector> route;

    public HttpSelector(IOptions<ReverseProxyOptions> options, ProxyLogger logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public async ValueTask<RouteConfig> MatchAsync(HttpContext context)
    {
        var req = context.Request;
        var path = req.Path.ToString();
        var host = req.Host.ToString();
#if DEBUG
        var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
        var r = await route.MatchAsync(host.Reverse(), path, context, MatchHttp);
#if DEBUG
        sw.Stop();
        logger.LogInformation($"{host} {path} match used: {sw.Elapsed}");
#endif
        if (r is null)
        {
            logger.NotFoundRouteHttp(host, path);
        }
        return r;
    }

    private bool MatchHttp(RouteConfig config, HttpContext context)
    {
        //todo
        return true;
    }

    public Task ReBuildAsync(IReadOnlyDictionary<string, RouteConfig> routes, CancellationToken cancellationToken)
    {
        var hostRouteBuilder = new RouteTableBuilder<RouteConfig, PathSelector>(StringComparison.OrdinalIgnoreCase, options.HttpRouteCahceSize);
        foreach (var route in routes.Values.Where(i => i.Match != null))
        {
            foreach (var host in route.Match.Hosts)
            {
                if (host.StartsWith("localhost:"))
                {
                    Set(hostRouteBuilder, route, $"127.0.0.1:{host.AsSpan(10)}");
                    Set(hostRouteBuilder, route, $"[::1]:{host.AsSpan(10)}");
                }
                Set(hostRouteBuilder, route, host);
            }
        }

        route = hostRouteBuilder.Build(RouteTableType.Complex);

        static void Set(RouteTableBuilder<RouteConfig, PathSelector> builder, RouteConfig? route, string host)
        {
            if (host.StartsWith('*'))
            {
                builder.Add(host[1..].Reverse(), route, RouteType.Prefix, route.Order);
            }
            else
            {
                builder.Add(host.Reverse(), route, RouteType.Exact, route.Order);
            }
        }

        return Task.CompletedTask;
    }

    public RouteConfig Match(HttpContext context)
    {
        var req = context.Request;
        var path = req.Path.ToString();
        var host = req.Host.ToString();
        var r = route.Match(host.Reverse(), path, context, MatchHttp);
        if (r is null)
        {
            logger.NotFoundRouteHttp(host, path);
        }
        return r;
    }
}