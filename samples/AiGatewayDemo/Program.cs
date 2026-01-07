using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKProxy.Middlewares.Http.Transforms;

var app = Host.CreateDefaultBuilder(args)
    .UseReverseProxy()
    .ConfigureServices(i =>
    {
        i.UseHttpMiddleware<AiGatewayMiddleware>();

        i.AddSingleton<ITransformProvider, AiGatewayTransformProvider>();
    })
    .Build();

await app.RunAsync();