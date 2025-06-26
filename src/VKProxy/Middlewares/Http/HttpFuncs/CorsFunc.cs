using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class CorsFunc : IHttpFunc
{
    public int Order => 0;

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        var cc = GetConfig(config);
        if (cc == null)
            return next;
        else return async c =>
        {
            var req = c.Request;
            if (HttpMethods.IsGet(req.Method))
            {
                SetCors(cc, c, true);
            }
            else if (HttpMethods.IsOptions(req.Method))
            {
                SetCors(cc, c, false);
                c.Response.StatusCode = StatusCodes.Status204NoContent;
                await c.Response.CompleteAsync();
                return;
            }

            await next(c);
        };
    }

    private static void SetCors(CorsFuncConfig config, HttpContext context, bool flag)
    {
        var req = context.Request;
        var resp = context.Response;
        if (config.AllowOrigin == "*")
        {
            if (flag) context.Items["AccessControlAllowOrigin"] = true;
            resp.Headers.AccessControlAllowOrigin = config.AllowOrigin;
        }
        else
        {
            var oo = req.Headers.Origin;
            var o = oo.ToString();
            if (config.AllowOriginRegex != null)
            {
                if (config.AllowOriginRegex.IsMatch(o))
                {
                    if (flag) context.Items["AccessControlAllowOrigin"] = true;
                    resp.Headers.AccessControlAllowOrigin = oo;
                }
                else
                    return;
            }
            else
            {
                if (config.AllowOrigin.Equals(o, StringComparison.OrdinalIgnoreCase))
                {
                    if (flag) context.Items["AccessControlAllowOrigin"] = true;
                    resp.Headers.AccessControlAllowOrigin = oo;
                }
                else
                    return;
            }
            resp.Headers.Vary = "Origin";
        }

        if (config.AllowHeaders == null)
        {
            resp.Headers.AccessControlAllowHeaders = req.Headers.AccessControlRequestHeaders;
        }
        else
        {
            resp.Headers.AccessControlAllowHeaders = config.AllowHeaders;
        }

        if (config.AllowMethods == null)
        {
            resp.Headers.AccessControlAllowMethods = req.Headers.AccessControlAllowMethods;
        }
        else
        {
            resp.Headers.AccessControlAllowMethods = config.AllowMethods;
        }

        if (config.AllowCredentials != null)
        {
            resp.Headers.AccessControlAllowCredentials = config.AllowCredentials;
        }

        if (config.MaxAge != null)
        {
            resp.Headers.AccessControlMaxAge = config.MaxAge;
        }

        if (config.ExposeHeaders != null)
        {
            resp.Headers.AccessControlExposeHeaders = config.ExposeHeaders;
        }
    }

    private static CorsFuncConfig GetConfig(RouteConfig config)
    {
        if (config.Metadata == null) return null;

        var c = new CorsFuncConfig();
        if (config.Metadata.TryGetValue("Access-Control-Allow-Origin", out var allowOrigin))
        {
            c.AllowOrigin = allowOrigin;
        }

        if (config.Metadata.TryGetValue("Access-Control-Allow-Origin-Regex", out var allowOriginRegex))
        {
            c.AllowOrigin = allowOriginRegex;
            c.AllowOriginRegex = new Regex(allowOriginRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        if (config.Metadata.TryGetValue("Access-Control-Allow-Headers", out var allowHeaders))
        {
            c.AllowHeaders = allowHeaders;
        }

        if (config.Metadata.TryGetValue("Access-Control-Allow-Methods", out var allowMethods))
        {
            c.AllowMethods = allowMethods;
        }

        if (config.Metadata.TryGetValue("Access-Control-Allow-Credentials", out var allowCredentials))
        {
            c.AllowCredentials = allowCredentials;
        }

        if (config.Metadata.TryGetValue("Access-Control-Max-Age", out var maxAge))
        {
            c.MaxAge = maxAge;
        }

        if (config.Metadata.TryGetValue("Access-Control-Expose-Headers", out var exposeHeaders))
        {
            c.ExposeHeaders = exposeHeaders;
        }

        if (c.AllowOrigin == null) return null; else return c;
    }

    internal record CorsFuncConfig
    {
        public string? AllowOrigin { get; set; }
        public Regex? AllowOriginRegex { get; set; }
        public string? AllowHeaders { get; set; }
        public string? AllowMethods { get; set; }
        public string? AllowCredentials { get; set; }
        public string? MaxAge { get; set; }
        public string? ExposeHeaders { get; set; }
    }
}