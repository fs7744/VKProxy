using DotNext.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VKProxy.ACME.Resource;
using VKProxy.Config;
using VKProxy.Middlewares.Http;

namespace VKProxy.ACME;

public interface IAcmeHttpClient
{
    Task<AcmeResponse<T>> GetAsync<T>(Uri directoryUri, CancellationToken cancellationToken);

    Task<AcmeResponse<T>> PostAsync<T>(Uri uri, object payload, CancellationToken cancellationToken);
}

public class DefaultAcmeHttpClient : IAcmeHttpClient
{
    private readonly IForwarderHttpClientFactory httpClientFactory;
    private HttpMessageInvoker? httpClient;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string MimeJoseJson = "application/jose+json";

    private static readonly IList<ProductInfoHeaderValue> userAgentHeaders = new[]
    {
        new ProductInfoHeaderValue("VKProxy.ACME", Assembly.GetExecutingAssembly().GetName().Version.ToString()),
        new ProductInfoHeaderValue(".NET", Environment.Version.ToString()),
    };

    public DefaultAcmeHttpClient(IForwarderHttpClientFactory httpClientFactory, AcmeOptions options)
    {
        this.httpClientFactory = httpClientFactory;
        Change(options.HttpClientConfig);
    }

    public void Change(HttpClientConfig httpClientConfig)
    {
        if (httpClient != null)
        {
            httpClient.Dispose();
        }
        this.httpClient = httpClientFactory.CreateHttpClient(httpClientConfig);
    }

    public async Task<AcmeResponse<T>> GetAsync<T>(Uri directoryUri, CancellationToken cancellationToken)
    {
        var req = new HttpRequestMessage() { Method = HttpMethod.Get, RequestUri = directoryUri };
        req.Headers.UserAgent.AddAll(userAgentHeaders);
        using var resp = await httpClient.SendAsync(req, cancellationToken);
        return await ProcessResponseAsync<T>(resp, cancellationToken);
    }

    public async Task<AcmeResponse<T>> PostAsync<T>(Uri uri, object payload, CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload, jsonSerializerOptions);
        var content = new StringContent(payloadJson, Encoding.UTF8, MimeJoseJson);
        // boulder will reject the request if sending charset=utf-8
        content.Headers.ContentType.CharSet = null;

        var req = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = uri,
            Content = content,
        };

        req.Headers.UserAgent.AddAll(userAgentHeaders);
        using var response = await httpClient.SendAsync(req, cancellationToken);
        return await ProcessResponseAsync<T>(response, cancellationToken);
    }

    private async Task<AcmeResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage resp, CancellationToken cancellationToken)
    {
        var location = resp.Headers.Location;
        var retryafter = (int)ExtractRetryAfterHeaderFromResponse(resp);
        var links = ExtractLinksFromResponse(resp);
        var nonce = resp.Headers.TryGetValues("Replay-Nonce", out var values) ? values.Single() : null;
        T? result = default;
        AcmeError error = null;

        if (resp.IsSuccessStatusCode)
        {
            result = await resp.Content.ReadFromJsonAsync<T>(jsonSerializerOptions, cancellationToken);
        }
        else
        {
            if (IsJson(resp.Content?.Headers?.ContentType?.MediaType))
            {
                error = await resp.Content.ReadFromJsonAsync<AcmeError>(jsonSerializerOptions, cancellationToken);
            }
            else
                resp.EnsureSuccessStatusCode();
        }
        return new AcmeResponse<T>(location, result, links, error, retryafter);
    }

    private bool IsJson(string? mediaType)
    {
        return mediaType != null && mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
    }

    private double ExtractRetryAfterHeaderFromResponse(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter != null)
        {
            var date = response.Headers.RetryAfter.Date;
            var delta = response.Headers.RetryAfter.Delta;
            if (date.HasValue)
                return Math.Abs((date.Value - DateTime.UtcNow).TotalSeconds);
            else if (delta.HasValue)
                return delta.Value.TotalSeconds;
        }

        return 0;
    }

    public static ILookup<string, Uri> ExtractLinksFromResponse(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Link", out var links) && links != null)
        {
            return links.Select(h =>
                {
                    var index = h.LastIndexOf('"', h.Length - 2);
                    var rel = h[(index + 1)..^1];
                    var url = h[1..(index - 6)];

                    return (
                        Rel: rel,
                        Uri: new Uri(url)
                    );
                })
                .ToLookup(l => l.Rel, l => l.Uri);
        }
        return null;
    }
}

public class AcmeOptions
{
    public HttpClientConfig? HttpClientConfig { get; set; }
}