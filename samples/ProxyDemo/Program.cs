using Microsoft.Extensions.Hosting;
using ProxyDemo;

var app = Host.CreateDefaultBuilder(args)
    .UseReverseProxy().ConfigureServices(i =>
    {
        i.UseUdpMiddleware<EchoUdpProxyMiddleware>();
    })
    .Build();

await app.RunAsync();