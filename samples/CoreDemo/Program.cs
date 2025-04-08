using CoreDemo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKProxy.Core.Hosting;

var app = Host.CreateDefaultBuilder(args).UseVKProxyCore()
    .ConfigureServices(i =>
    {
        i.AddSingleton<IListenHandler, ListenHandler>();
    })
    .Build();

await app.RunAsync();