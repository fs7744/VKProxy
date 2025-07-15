using System.Net.Http.Json;
using VKProxy.Config;
using VKProxy.Middlewares.Http;

namespace VKProxy.ACME;

public interface IAcmeHttpClient
{
    Task<(HttpResponseMessage, T?)> GetAsync<T>(Uri directoryUri, CancellationToken cancellationToken);
}

public class DefaultAcmeHttpClient : IAcmeHttpClient
{
    private readonly IForwarderHttpClientFactory httpClientFactory;
    private HttpMessageInvoker? httpClient;

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

    public async Task<(HttpResponseMessage, T?)> GetAsync<T>(Uri directoryUri, CancellationToken cancellationToken)
    {
        var resp = await httpClient.SendAsync(new HttpRequestMessage() { Method = HttpMethod.Get, RequestUri = directoryUri }, cancellationToken);
        resp.EnsureSuccessStatusCode();
        return (resp, await resp.Content.ReadFromJsonAsync<T>(cancellationToken));
    }
}

public class AcmeOptions
{
    public HttpClientConfig? HttpClientConfig { get; set; }
}