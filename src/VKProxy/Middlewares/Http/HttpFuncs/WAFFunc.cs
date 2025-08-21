using Microsoft.AspNetCore.Http;
using VKProxy.Config;
using VKProxy.Core.Loggers;
using VKProxy.HttpRoutingStatement;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class WAFFunc : IHttpFunc
{
    private readonly ProxyLogger logger;
    private readonly IRouteStatementFactory statementFactory;

    public int Order => -100;

    public WAFFunc(ProxyLogger logger, IRouteStatementFactory statementFactory)
    {
        this.logger = logger;
        this.statementFactory = statementFactory;
    }

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        var cc = GetConfig(config);
        if (cc == null)
            return next;
        else return async c =>
        {
            foreach (var (h, f) in cc.AsSpan())
            {
                if (f(c))
                {
                    c.Response.Headers["x-waf"] = h;
                    c.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await c.Response.CompleteAsync();
                    return;
                }
            }
            await next(c);
        };
    }

    private KeyValuePair<string, Func<HttpContext, bool>>[] GetConfig(RouteConfig config)
    {
        if (config.Metadata == null) return null;
        var list = new List<KeyValuePair<string, Func<HttpContext, bool>>>();
        foreach (var (k, v) in config.Metadata)
        {
            if (k.StartsWith("waf_", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var f = statementFactory.ConvertToFunction(v);
                    list.Add(new KeyValuePair<string, Func<HttpContext, bool>>(k, f));
                }
                catch (Exception ex)
                {
                    logger.ErrorConfig(ex.Message);
                }
            }
        }
        return list.Count > 0 ? list.ToArray() : null;
    }
}