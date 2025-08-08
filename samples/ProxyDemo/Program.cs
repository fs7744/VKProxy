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
        //o.Meters = new string[] { "System.Net.Http", "System.Net.NameResolution", "System.Runtime", "Microsoft.AspNetCore.Server.Kestrel", "Microsoft.AspNetCore.Server.Kestrel.Udp", "Microsoft.AspNetCore.MemoryPool", "VKProxy.ReverseProxy" };
    }, j =>
    {
        j.WithTracing(i =>
        {
            i.AddConsoleExporter();
        })
        .WithLogging(i => i.AddConsoleExporter());
    })
    .ConfigureServices(i =>
        {
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
