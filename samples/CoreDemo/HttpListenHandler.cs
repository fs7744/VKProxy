using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using System.Net;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;

namespace CoreDemo;

public class HttpListenHandler : ListenHandlerBase
{
    private readonly ILogger<HttpListenHandler> logger;
    private readonly ICertificateLoader certificateLoader;

    public HttpListenHandler(ILogger<HttpListenHandler> logger, ICertificateLoader certificateLoader)
    {
        this.logger = logger;
        this.certificateLoader = certificateLoader;
    }

    private async Task Proxy(HttpContext context)
    {
        var resp = context.Response;
        if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            resp.Headers.Origin = "*";
            resp.Headers.AccessControlAllowOrigin = "*";
            return;
        }

        if (string.Equals(context.Request.Path, "/testhttp", StringComparison.OrdinalIgnoreCase))
        {
            resp.Headers.ContentType = "text/html";
            resp.Headers["x-p"] = context.Request.Protocol;
            await resp.WriteAsync("""
                <!DOCTYPE html>
                <html>
                <body>
                <p id="demo">Fetch a file to change this text.</p>
                <script>

                fetch('https://127.0.0.1:4001/api', {
                  method: 'POST',
                  headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'Origin': 'https://127.0.0.1:4001'
                  },
                  protocol: 'http3',
                })
                .then(response => {
                  if (!response.ok) {
                    throw new Error('Network response was not ok');
                  }
                  return response.json();
                })
                .then(data => {
                document.getElementById("demo").innerHTML =data.protocol;
                  console.log(data);
                })
                .catch(error => {
                  console.error('There was a problem with the fetch operation:', error);
                });

                </script>
                </body>
                </html>

                """);
            return;
        }

        //resp.StatusCode = 404;
        await resp.WriteAsJsonAsync(new { context.Request.Protocol });
        await resp.CompleteAsync().ConfigureAwait(false);
    }

    public override async Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        try
        {
            var ip = new EndPointOptions()
            {
                EndPoint = IPEndPoint.Parse("127.0.0.1:4000"),
                Key = "http"
            };
            await transportManager.BindHttpAsync(ip, Proxy, cancellationToken);
            logger.LogInformation($"listen {ip.EndPoint}");
            ip = new EndPointOptions()
            {
                EndPoint = IPEndPoint.Parse("127.0.0.1:4001"),
                Key = "https"
            };

            var (c, f) = certificateLoader.LoadCertificate(new CertificateConfig() { Path = "testCert.pfx", Password = "testPassword" });
            await transportManager.BindHttpAsync(ip, Proxy, cancellationToken, HttpProtocols.Http1AndHttp2AndHttp3, callbackOptions: new HttpsConnectionAdapterOptions()
            {
                //ServerCertificateSelector = (context, host) => c
                ServerCertificate = c,
                CheckCertificateRevocation = false,
                ClientCertificateMode = ClientCertificateMode.AllowCertificate
            });
            logger.LogInformation($"listen {ip.EndPoint}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
}