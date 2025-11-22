using Lmzzz.AspNetCoreTemplate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VKProxy.Config;
using VKProxy.Core.Loggers;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class ContentFunc : IHttpFunc
{
    public int Order => 30;
    private readonly ProxyLogger logger;
    private readonly ITemplateEngineFactory statementFactory;

    public ContentFunc(ProxyLogger logger, ITemplateEngineFactory statementFactory)
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
            foreach (var cf in cc.AsSpan())
            {
                if (cf.Func(c))
                {
                    c.Response.Headers.ContentType = cf.ContentType;
                    c.Response.StatusCode = StatusCodes.Status200OK;
                    await c.Response.WriteAsync(cf.Content);
                    await c.Response.CompleteAsync();
                    return;
                }
            }
            await next(c);
        };
    }

    private ContentConfig[] GetConfig(RouteConfig config)
    {
        if (config.Metadata == null) return null;
        var list = new List<ContentConfig>();
        foreach (var (k, v) in config.Metadata.Where(i => i.Key.EndsWith("_Content")))
        {
            try
            {
                var c = new ContentConfig() { Content = v };
                if (config.Metadata.TryGetValue($"{k}Type", out var t))
                {
                    c.ContentType = t;
                }
                else
                    c.ContentType = "text/plain";
                if (config.Metadata.TryGetValue($"{k}When", out var when))
                {
                    c.Func = statementFactory.ConvertRouteFunction(when);
                }
                else
                    c.Func = static context => true;
                list.Add(c);
            }
            catch (Exception ex)
            {
                logger.ErrorConfig(ex.Message);
            }
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

    internal class ContentConfig
    {
        internal Func<HttpContext, bool> Func;
        internal StringValues ContentType;
        internal string Content;
    }
}