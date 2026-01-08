using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKProxy;
using VKProxy.Middlewares.Http.Transforms;

var app = VKProxyHost.CreateBuilder(args)
    .ConfigureServices(i =>
    {
        i.UseHttpMiddleware<AiGatewayMiddleware>();

        i.AddSingleton<ITransformProvider, AiGatewayTransformProvider>();
        i.AddKeyedSingleton<IAIProvider, OpenAiProvider>("openai");
    })
    .Build();

await app.RunAsync();