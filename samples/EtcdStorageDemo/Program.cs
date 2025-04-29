using Microsoft.Extensions.Hosting;
using VKProxy.Storages.Etcd;

var app = Host.CreateDefaultBuilder(args)
    .UseReverseProxy()
    .ConfigureServices(i =>
    {
        i.UseSocks5();
        i.UseEtcdConfig(o =>
        {
            o.ConnectionString = "https://root:rootpwd@localhost:2379";
            o.UseInsecureChannel = true;
        });
    })
    .Build();

await app.RunAsync();