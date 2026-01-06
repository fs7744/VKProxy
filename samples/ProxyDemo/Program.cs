using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ProxyDemo;
using ProxyDemo.IDestinationResolvers;
using ProxyDemo.Transforms;
using VKProxy;
using VKProxy.Middlewares.Http.Transforms;
using VKProxy.ServiceDiscovery;

var app = VKProxyHost.CreateBuilder(args, (_, o) =>
    {
        o.UseSocks5 = true;
        o.Sampler = VKProxy.Sampler.Random;
        o.Exporter = "otlp";
        //o.Meters = new string[] { "System.Net.Http", "System.Net.NameResolution", "System.Runtime", "Microsoft.AspNetCore.Server.Kestrel", "Microsoft.AspNetCore.Server.Kestrel.Udp", "Microsoft.AspNetCore.MemoryPool", "VKProxy.ReverseProxy" };
    }
    //, j =>
    //{
    //    //j.UseOtlpExporter();

    //    j.WithTracing(i =>
    //    {
    //        i.AddConsoleExporter();
    //    });
    //    //j.WithLogging(i => i.AddConsoleExporter());
    //}
    )
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