using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class JwtCheckFunc : IHttpFunc
{
    public int Order => -90;

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        if (config.Metadata == null || !config.Metadata.TryGetValue("JwtSigningKey", out var v) || string.IsNullOrWhiteSpace(v)) return next;
        var jsonWebTokenHandler = new JsonWebTokenHandler();
        var p = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(v))
        };
        if (config.Metadata.TryGetValue("JwtIssuer", out v) && !string.IsNullOrWhiteSpace(v))
        {
            p.ValidateIssuer = true;
            p.ValidIssuer = v;
        }
        if (config.Metadata.TryGetValue("JwtAudience", out v) && !string.IsNullOrWhiteSpace(v))
        {
            p.ValidateAudience = true;
            p.ValidAudience = v;
        }
        var header = "Authorization";
        if (config.Metadata.TryGetValue("JwtHeader", out v) && !string.IsNullOrWhiteSpace(v))
        {
            header = v;
        }
        var headerPrefix = "Bearer ";
        if (config.Metadata.TryGetValue("JwtHeaderPrefix", out v) && v != null)
        {
            headerPrefix = v;
        }
        if (config.Metadata.TryGetValue("JwtLifetime", out v) && bool.TryParse(v, out var vv))
        {
            p.ValidateLifetime = vv;
        }
        var returnException = false;
        if (config.Metadata.TryGetValue("JwtExceptionDetail", out v) && bool.TryParse(v, out vv))
        {
            returnException = vv;
        }
        return async c =>
        {
            if (c.Request.Headers.TryGetValue(header, out var h) && !StringValues.IsNullOrEmpty(h))
            {
                var s = h.First();
                if (s != null && s.StartsWith(headerPrefix))
                {
                    s = s[headerPrefix.Length..];
                    var r = await jsonWebTokenHandler.ValidateTokenAsync(s, p);
                    if (r.IsValid)
                    {
                        await next(c);
                        return;
                    }
                    else if (returnException)
                    {
                        c.Response.Headers["x-jwt-error"] = r.Exception?.Message ?? "Invalid JWT token";
                    }
                }
            }
            c.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await c.Response.CompleteAsync();
            return;
        };
    }
}