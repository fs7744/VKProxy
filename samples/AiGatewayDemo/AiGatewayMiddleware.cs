using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VKProxy.Config;
using VKProxy.Features;
using VKProxy.LoadBalancing;
using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public class AiGatewayMiddleware : IMiddleware
{
    private readonly ILogger<AiGatewayMiddleware> logger;
    private readonly IConfigSource<IProxyConfig> configSource;
    private readonly IServiceProvider provider;
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    public static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public AiGatewayMiddleware(ILogger<AiGatewayMiddleware> logger, IConfigSource<IProxyConfig> configSource, IServiceProvider provider, ILoadBalancingPolicyFactory loadBalancing)
    {
        this.logger = logger;
        this.configSource = configSource;
        this.provider = provider;
        this.loadBalancing = loadBalancing;
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
                    driver = provider.GetKeyedService<IAIProvider>(ai.Driver);
                }
                else
                    driver = null;

                if (driver is null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = $"Unsupported AI provider: {req.Provider}" }, context.RequestAborted);
                    return;
                }

                if (configSource.CurrentSnapshot.Clusters.TryGetValue(ai.Driver, out var cluster))
                {
                    feature.SelectedDestination = loadBalancing.PickDestination(feature, cluster);
                    if (feature.SelectedDestination is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        await context.Response.WriteAsJsonAsync(new { error = $"{req.Provider} service unavailable" }, context.RequestAborted);
                        return;
                    }
                }
                await SendAi(context, req, ai, driver);

                var origin = context.Response.Body;
                using var buf = new ResponseCachingStream(origin, 64 * 1024 * 1024 * 10, 81920, () => ValueTask.CompletedTask);
                context.Response.Body = buf;

                await next(context);

                var resp = buf.GetCachedResponseBody();
                using var u = new MemoryStream(resp.Segments.SelectMany(i => i).ToArray());
                var aiResp = JsonSerializer.Deserialize<AiResponse>(u, jsonOptions);
                if (aiResp?.Usage != null)
                {
                    logger.LogWarning("AI TotalTokens: {Usage}", aiResp.Usage.TotalTokens);
                }
                context.Response.Body = origin;
                return;
            }
        }
        await next(context);
    }

    private async Task SendAi(HttpContext context, AiRequest req, AiMapping ai, IAIProvider driver)
    {
        await driver.Request(context, req, ai);
    }
}

public interface IAIProvider
{
    Task Request(HttpContext context, AiRequest req, AiMapping ai);
}

public class OpenAiProvider : IAIProvider
{
    public virtual async Task Request(HttpContext context, AiRequest req, AiMapping ai)
    {
        if (req.Model == null)
        {
            req.Model = ai.DefaultModel;
        }

        context.Request.Headers.Authorization = $"Bearer {req.ApiKey ?? ai.ApiKey}";

        req.Provider = null;
        req.ApiKey = null;
        var r = new MemoryStream(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(req, AiGatewayMiddleware.jsonOptions)));
        context.Request.Body = r;

        context.Request.ContentLength = r.Length;
    }
}

public class AiMapping
{
    public string DefaultModel { get; set; }
    public string ApiKey { get; set; }
    public string? Driver { get; set; }
}

public class AiRequest
{
    public string? ApiKey { get; set; }
    public bool? Stream { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public List<AiMessage>? Messages { get; set; }
}

public class AiMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class AiResponse
{
    public AiResponseUsage? Usage { get; set; }
}

public class AiResponseUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int? PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; set; }
}