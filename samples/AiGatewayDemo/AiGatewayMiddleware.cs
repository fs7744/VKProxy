using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VKProxy.Config;
using VKProxy.Features;

public class AiGatewayMiddleware : IMiddleware
{
    private readonly ILogger<AiGatewayMiddleware> logger;
    private readonly IConfigSource<IProxyConfig> configSource;
    private readonly IServiceProvider provider;

    public AiGatewayMiddleware(ILogger<AiGatewayMiddleware> logger, IConfigSource<IProxyConfig> configSource, IServiceProvider provider)
    {
        this.logger = logger;
        this.configSource = configSource;
        this.provider = provider;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var feature = context.Features.Get<IReverseProxyFeature>();
        if (feature is not null)
        {
            var route = feature.Route;
            if (route is not null && route.Metadata is not null
                && route.Metadata.TryGetValue("AiMapping", out var b))
            {
                var req = await context.Request.ReadFromJsonAsync<AiRequest>(context.RequestAborted);
                if (req is null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
                var mapping = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, AiMapping>>(b);

                IAIProvider driver;

                if (mapping.TryGetValue(req.Provider ?? "openai", out var ai))
                {
                    driver = provider.GetKeyedService<IAIProvider>(ai.Driver ?? "openai");
                }
                else
                    driver = null;

                if (driver is null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = $"Unsupported AI provider: {req.Provider}" }, context.RequestAborted);
                    return;
                }

                await SendAi(context, req, ai, driver);

                return;
            }
        }
        await next(context);
    }

    private async Task SendAi(HttpContext context, AiRequest req, AiMapping ai, IAIProvider driver)
    {
        if (req.UseStream == true)
        {
            //
        }
        else
        {
            context.Request.Headers["Authorization"] = $"Bearer {ai.ApiKey}";
        }
    }
}

public interface IAIProvider
{

}

public class AiMapping
{
    public string DefaultModel { get; set; }
    public string ApiKey { get; set; }
    public string? Driver { get; set; }
}

public class AiRequest
{
    public bool? UseStream { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public List<AiMessage>? Messages { get; set; }
}

public class AiMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}