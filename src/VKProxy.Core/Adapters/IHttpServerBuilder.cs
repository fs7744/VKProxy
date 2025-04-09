using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace VKProxy.Core.Adapters;

public interface IHttpServerBuilder
{
    public IConnectionBuilder UseHttpServer(IConnectionBuilder builder, IHttpApplication<HttpApplication.Context> application, HttpProtocols protocols, bool addAltSvcHeader);

    public IMultiplexedConnectionBuilder UseHttp3Server(IMultiplexedConnectionBuilder builder, IHttpApplication<HttpApplication.Context> application, HttpProtocols protocols, bool addAltSvcHeader);
}