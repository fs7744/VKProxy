using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class OnlyHttpsFunc : IHttpFunc
{
    public int Order => -1000;

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        if (config.Metadata == null || !config.Metadata.TryGetValue("OnlyHttps", out var v) || !bool.TryParse(v, out var b) || !b) return next;
        return c =>
        {
            if (c.Request.IsHttps)
            {
                return next(c);
            }
            else
            {
                c.Response.Redirect($"https://{c.Request.Host}{c.Request.GetEncodedPathAndQuery()}", true);
                return c.Response.CompleteAsync();
            }
        };
    }
}