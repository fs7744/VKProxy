using Microsoft.Extensions.Hosting;
using VKProxy.Storages.Etcd;

var app = Host.CreateDefaultBuilder(args)
    .UseReverseProxy()
    .UseEtcdConfig()
    .Build();

await app.RunAsync();