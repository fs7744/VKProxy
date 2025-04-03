using Microsoft.Extensions.Hosting;

var app = Host.CreateDefaultBuilder(args).UseVKProxyCore()
    .Build();

await app.RunAsync();