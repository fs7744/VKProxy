using Microsoft.AspNetCore.Connections;
using System.Net;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp.Client;

namespace VKProxy.Core.Sockets.Udp;

public sealed class UdpTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private readonly GeneralLogger logger;
    private readonly IUdpConnectionFactory connectionFactory;

    public UdpTransportFactory(
        GeneralLogger logger,
        IUdpConnectionFactory connectionFactory)
    {
        this.logger = logger;
        this.connectionFactory = connectionFactory;
    }

    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var transport = new UdpConnectionListener(endpoint, logger, connectionFactory);
        transport.Bind();
        return new ValueTask<IConnectionListener>(transport);
    }

    public bool CanBind(EndPoint endpoint)
    {
        return endpoint switch
        {
            UdpEndPoint _ => true,
            _ => false
        };
    }
}