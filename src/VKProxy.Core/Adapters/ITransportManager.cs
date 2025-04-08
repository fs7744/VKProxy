using Microsoft.AspNetCore.Connections;
using System.Net;
using VKProxy.Core.Config;

namespace VKProxy.Core.Adapters;

public interface ITransportManager
{
    Task<EndPoint> BindAsync(EndPointOptions endpointConfig, ConnectionDelegate connectionDelegate, CancellationToken cancellationToken);

    Task<EndPoint> BindAsync(EndPointOptions endpointConfig, MultiplexedConnectionDelegate multiplexedConnectionDelegate, CancellationToken cancellationToken);

    Task StopEndpointsAsync(List<EndPointOptions> endpointsToStop, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}