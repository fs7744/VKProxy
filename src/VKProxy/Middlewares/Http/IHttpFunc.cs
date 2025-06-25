using Microsoft.AspNetCore.Http;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http;

public interface IHttpFunc
{
    int Order { get; }

    RequestDelegate Create(RouteConfig config, RequestDelegate next);
}