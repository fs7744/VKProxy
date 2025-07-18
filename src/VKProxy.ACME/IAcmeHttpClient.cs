using DotNext.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;
using VKProxy.Config;
using VKProxy.Middlewares.Http;

namespace VKProxy.ACME;

public interface IAcmeHttpClient
{
    Task<AcmeResponse<T>> HeadAsync<T>(Uri directoryUri, CancellationToken cancellationToken);

    Task<AcmeResponse<T>> GetAsync<T>(Uri directoryUri, CancellationToken cancellationToken);

    Task<AcmeResponse<T>> PostAsync<T>(Uri uri, object payload, CancellationToken cancellationToken);

    Task<AcmeResponse<T>> PostAsync<T>(JwsSigner jwsSigner, Uri location, object entity,
            Func<CancellationToken, Task<string>> consumeNonce,
            Uri keyId = null, int retryCount = 1, CancellationToken cancellationToken = default);
}

public class DefaultAcmeHttpClient : IAcmeHttpClient
{
    private readonly IForwarderHttpClientFactory httpClientFactory;
    private HttpMessageInvoker? httpClient;

    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
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
        return await SendAsync<T>(HttpMethod.Get, directoryUri, cancellationToken);
    }

    public async Task<AcmeResponse<T>> HeadAsync<T>(Uri directoryUri, CancellationToken cancellationToken)
    {
        return await SendAsync<T>(HttpMethod.Head, directoryUri, cancellationToken);
    }

    private async Task<AcmeResponse<T>> SendAsync<T>(HttpMethod method, Uri directoryUri, CancellationToken cancellationToken)
    {
        var req = new HttpRequestMessage() { Method = method, RequestUri = directoryUri };
        req.Headers.UserAgent.AddAll(userAgentHeaders);
        using var resp = await httpClient.SendAsync(req, cancellationToken);
        return await ProcessResponseAsync<T>(resp, cancellationToken);
    }

    public async Task<AcmeResponse<T>> PostAsync<T>(Uri uri, object payload, CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions);
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

    public async Task<AcmeResponse<T>> PostAsync<T>(
            JwsSigner jwsSigner,
            Uri location,
            object entity,
            Func<CancellationToken, Task<string>> consumeNonce,
            Uri keyId = null,
            int retryCount = 1, CancellationToken cancellationToken = default)
    {
        var payload = jwsSigner.Sign(entity, keyId, url: location, nonce: await consumeNonce(cancellationToken));
        var response = await PostAsync<T>(location, payload, cancellationToken);

        while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
            response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
            retryCount-- > 0)
        {
            payload = jwsSigner.Sign(entity, keyId, url: location, nonce: await consumeNonce(cancellationToken));
            response = await PostAsync<T>(location, payload, cancellationToken);
        }

        if (response.Error != null)
        {
            throw new AcmeException(
                string.Format("Fail to load resource from '{0}'.", location),
                response.Error);
        }

        return response;
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
            if (typeof(T) == typeof(string))
            {
                result = (T)(object)(await resp.Content.ReadAsStringAsync(cancellationToken));
            }
            else if (IsJson(resp.Content?.Headers?.ContentType?.MediaType))
            {
                var s = await resp.Content.ReadAsStringAsync(cancellationToken);
                result = JsonSerializer.Deserialize<T>(s, JsonSerializerOptions);
                //result = await resp.Content.ReadFromJsonAsync<T>(JsonSerializerOptions, cancellationToken);
            }
        }
        else
        {
            if (IsJson(resp.Content?.Headers?.ContentType?.MediaType))
            {
                error = await resp.Content.ReadFromJsonAsync<AcmeError>(JsonSerializerOptions, cancellationToken);
            }
            else
                resp.EnsureSuccessStatusCode();
        }
        return new AcmeResponse<T>(location, result, links, nonce, error, retryafter);
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