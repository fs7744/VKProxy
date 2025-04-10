using Microsoft.Extensions.Hosting;

var app = Host.CreateDefaultBuilder(args).UseReverseProxy()
    .Build();

await app.RunAsync();