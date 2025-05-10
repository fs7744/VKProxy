using Microsoft.Extensions.Hosting;
using VKProxy.Storages.Etcd;

var app = Host.CreateDefaultBuilder(args)
    .UseReverseProxy()
    .ConfigureServices(i =>
    {
        i.UseEtcdConfigFromEnv();
    })
    .Build();

await app.RunAsync();