using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProxyDemo;
using ProxyDemo.IDestinationResolvers;
using ProxyDemo.Transforms;
using System.Diagnostics;
using System.Xml.Linq;
using VKProxy;
using VKProxy.Middlewares.Http;
using VKProxy.Middlewares.Http.Transforms;
using VKProxy.ServiceDiscovery;

var app = VKProxyHost.CreateBuilder(args, (_, o) =>
    {
        o.UseSocks5 = true;
        o.Sampler = VKProxy.Sampler.Random;
    })
    .ConfigureServices(i =>
    {
        i.AddOpenTelemetry()
        .WithTracing(i =>
        {
            if (i is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
            {
                deferredTracerProviderBuilder.Configure((sp, builder) =>
                {
                    var activitySourceService = sp.GetService<ActivitySource>();
                    if (activitySourceService != null)
                    {
                        builder.AddSource(activitySourceService.Name);
                    }
                });
            }
            i.AddConsoleExporter();
        })
        .WithLogging(i => i.AddConsoleExporter())
    .ConfigureResource(resource => resource.AddHostDetector().AddOperatingSystemDetector().AddContainerDetector())
    .WithMetrics(builder => builder.AddMeter("System.Runtime")
    //.AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("System.Net.Http")
            .AddMeter("System.Net.NameResolution")
             .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
             .AddMeter("Microsoft.AspNetCore.Server.Kestrel.Udp")
             //.AddMeter("Microsoft.AspNetCore.Http.Connections")
             //.AddMeter("Microsoft.AspNetCore.Routing")
             //.AddMeter("Microsoft.AspNetCore.Diagnostics")
             .AddMeter("Microsoft.AspNetCore.MemoryPool")
             .AddMeter("VKProxy.ReverseProxy")
             .AddPrometheusExporter());

        i.ConfigureOpenTelemetryMeterProvider(j =>
        {
            //j.AddView("kestrel.connection.duration", MetricStreamConfiguration.Drop);
            //j.AddView("kestrel.tls_handshake.duration", MetricStreamConfiguration.Drop);
            //j.AddView("aspnetcore.memory_pool.total_allocated", MetricStreamConfiguration.Drop);
        });
        i.AddSingleton<IHttpFunc, PrometheusFunc>();
        //i.Configure<ReverseProxyOptions>(o => o.Section = "TextSection");
        //i.UseUdpMiddleware<EchoUdpProxyMiddleware>();
        i.UseHttpMiddleware<EchoHttpMiddleware>();

        i.AddSingleton<IDestinationResolver, StaticDNS>();
        i.AddSingleton<IDestinationResolver, NonStaticDNS>();
        i.AddSingleton<ITransformProvider, TestITransformProvider>();
        i.AddSingleton<ITransformFactory, TestTransformFactory>();
    })
    .Build();

await app.RunAsync();