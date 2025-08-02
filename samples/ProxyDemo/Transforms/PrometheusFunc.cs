using Microsoft.AspNetCore.Http;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using VKProxy.Config;
using VKProxy.Middlewares.Http;

namespace ProxyDemo.Transforms;

internal class PrometheusFunc : IHttpFunc
{
    private readonly object o;
    private readonly RequestDelegate func;

    public int Order => 30;

    public PrometheusFunc(MeterProvider meterProvider)
    {
        RequestDelegate next = c => Task.CompletedTask;
        var t = typeof(PrometheusAspNetCoreOptions).Assembly.GetTypes().FirstOrDefault(t => t.Name == "PrometheusExporterMiddleware");
        this.o = t.GetConstructors().First().Invoke(new object[] { meterProvider, next });
        var func = t.GetMethods().First(i => i.Name == "InvokeAsync").CreateDelegate<Func<HttpContext, Task>>(o);
        this.func = c => func(c);
    }

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        if (config.Metadata != null && config.Metadata.TryGetValue("Prometheus", out var value) && bool.TryParse(value, out var b) && b)
        {
            return func;
        }
        else
            return next;
    }
}