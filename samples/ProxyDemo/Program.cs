using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProxyDemo;
using VKProxy;

var app = Host.CreateDefaultBuilder(args)
    .ConfigureServices(i =>
    {
        //i.Configure<ReverseProxyOptions>(o => o.Section = "TextSection");
        i.UseUdpMiddleware<EchoUdpProxyMiddleware>();
        //i.UseHttpMiddleware<EchoHttpMiddleware>();
        i.UseSocks5();
        i.UseWSToSocks5();
    })
    .UseReverseProxy()
    .Build();

await app.RunAsync();