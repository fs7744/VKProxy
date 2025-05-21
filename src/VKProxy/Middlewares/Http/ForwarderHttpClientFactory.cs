using System.Diagnostics;
using System.Net;
using System.Text;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http;

public interface IForwarderHttpClientFactory
{
    HttpMessageInvoker? CreateHttpClient(HttpClientConfig httpClientConfig);
}

public class ForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    public HttpMessageInvoker? CreateHttpClient(HttpClientConfig httpClientConfig)
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            EnableMultipleHttp2Connections = true,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15),
        };
        ConfigureHandler(httpClientConfig, handler);
        return new HttpMessageInvoker(handler, disposeHandler: true);
    }

    protected void ConfigureHandler(HttpClientConfig newConfig, SocketsHttpHandler handler)
    {
        if (newConfig == null) return;
        if (newConfig.SslProtocols.HasValue)
        {
            handler.SslOptions.EnabledSslProtocols = newConfig.SslProtocols.Value;
        }
        if (newConfig.MaxConnectionsPerServer is not null)
        {
            handler.MaxConnectionsPerServer = newConfig.MaxConnectionsPerServer.Value;
        }
        if (newConfig.DangerousAcceptAnyServerCertificate ?? false)
        {
            handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
        }

        handler.EnableMultipleHttp2Connections = newConfig.EnableMultipleHttp2Connections.GetValueOrDefault(true);
        handler.EnableMultipleHttp3Connections = newConfig.EnableMultipleHttp3Connections.GetValueOrDefault(true);
        handler.AllowAutoRedirect = newConfig.AllowAutoRedirect.GetValueOrDefault(false);

        if (newConfig.RequestHeaderEncoding is not null)
        {
            var encoding = Encoding.GetEncoding(newConfig.RequestHeaderEncoding);
            handler.RequestHeaderEncodingSelector = (_, _) => encoding;
        }

        if (newConfig.ResponseHeaderEncoding is not null)
        {
            var encoding = Encoding.GetEncoding(newConfig.ResponseHeaderEncoding);
            handler.ResponseHeaderEncodingSelector = (_, _) => encoding;
        }

        var webProxy = TryCreateWebProxy(newConfig.WebProxy);
        if (webProxy is not null)
        {
            handler.Proxy = webProxy;
            handler.UseProxy = true;
        }
    }

    private static WebProxy? TryCreateWebProxy(WebProxyConfig? webProxyConfig)
    {
        if (webProxyConfig is null || webProxyConfig.Address is null)
        {
            return null;
        }

        var webProxy = new WebProxy(webProxyConfig.Address);

        webProxy.UseDefaultCredentials = webProxyConfig.UseDefaultCredentials.GetValueOrDefault(false);
        webProxy.BypassProxyOnLocal = webProxyConfig.BypassOnLocal.GetValueOrDefault(false);

        return webProxy;
    }
}