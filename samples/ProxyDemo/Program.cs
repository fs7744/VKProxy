using Microsoft.Extensions.Hosting;
using ProxyDemo;

var app = Host.CreateDefaultBuilder(args)
    .UseReverseProxy()
    .ConfigureServices(i =>
    {
        i.UseUdpMiddleware<EchoUdpProxyMiddleware>();
        i.UseHttpMiddleware<EchoHttpMiddleware>();
    })
    .Build();

await app.RunAsync();