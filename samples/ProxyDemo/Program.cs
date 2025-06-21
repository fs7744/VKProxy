using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProxyDemo;
using ProxyDemo.IDestinationResolvers;
using ProxyDemo.Transforms;
using VKProxy;
using VKProxy.Middlewares.Http.Transforms;
using VKProxy.ServiceDiscovery;

var app = VKProxyHost.CreateBuilder(new VKProxyHostOptions() { UseSocks5 = true, Sampler = Sampler.Random })
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