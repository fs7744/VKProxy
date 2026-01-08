using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKProxy;

var app = VKProxyHost.CreateBuilder(args)
    .ConfigureServices(i =>
    {
        i.UseHttpMiddleware<AiGatewayMiddleware>();
        i.AddKeyedSingleton<IAIProvider, OpenAiProvider>("openai");
    })
    .Build();

await app.RunAsync();