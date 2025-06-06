using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProxyDemo;
using ProxyDemo.IDestinationResolvers;
using ProxyDemo.Transforms;
using VKProxy;
using VKProxy.Middlewares.Http.Transforms;
using VKProxy.ServiceDiscovery;

var app = Host.CreateDefaultBuilder(args)
    .ConfigureServices(i =>
    {
        //i.Configure<ReverseProxyOptions>(o => o.Section = "TextSection");
        //i.UseUdpMiddleware<EchoUdpProxyMiddleware>();
        i.UseHttpMiddleware<EchoHttpMiddleware>();
        i.UseSocks5();
        i.UseWSToSocks5();

        i.AddSingleton<IDestinationResolver, StaticDNS>();
        i.AddSingleton<IDestinationResolver, NonStaticDNS>();
        i.AddSingleton<ITransformProvider, TestITransformProvider>();
        i.AddSingleton<ITransformFactory, TestTransformFactory>();
    })
    .UseReverseProxy()
    .Build();

await app.RunAsync();