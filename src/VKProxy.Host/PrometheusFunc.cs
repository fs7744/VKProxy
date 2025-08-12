using Microsoft.AspNetCore.Http;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class PrometheusFunc : IHttpFunc
{
    private readonly object o;
    private readonly RequestDelegate func;

    public int Order => 30;

    public PrometheusFunc(MeterProvider meterProvider)
    {
        RequestDelegate next = c => Task.CompletedTask;
        var t = typeof(PrometheusAspNetCoreOptions).Assembly.GetTypes().FirstOrDefault(t => t.Name == "PrometheusExporterMiddleware");
        var i = t.GetConstructor(new Type[] { typeof(MeterProvider), typeof(Microsoft.AspNetCore.Http.RequestDelegate) });
        this.o = i.Invoke(new object[] { meterProvider, next });
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