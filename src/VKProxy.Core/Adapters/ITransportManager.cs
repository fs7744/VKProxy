using Microsoft.AspNetCore.Connections;
using System.Net;
using VKProxy.Core.Config;

namespace VKProxy.Core.Adapters;

public interface ITransportManager
{
    Task<EndPoint> BindAsync(EndPointOptions endpointConfig, ConnectionDelegate connectionDelegate, CancellationToken cancellationToken);

    Task<EndPoint> BindAsync(EndPointOptions endpointConfig, MultiplexedConnectionDelegate multiplexedConnectionDelegate, CancellationToken cancellationToken);

    /// <summary>
    /// EndPointOptions will generate Kestrel EndpointConfig becase of Kestrel need, so it can't effect old one when you new one EndPointOptions to do stop
    /// </summary>
    /// <param name="endpointsToStop"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StopEndpointsAsync(List<EndPointOptions> endpointsToStop, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}