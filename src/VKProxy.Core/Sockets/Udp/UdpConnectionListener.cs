using DotNext;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using VKProxy.Core.Loggers;
using VKProxy.Core.Sockets.Udp.Client;

namespace VKProxy.Core.Sockets.Udp;

internal sealed class UdpConnectionListener : IConnectionListener
{
    private EndPoint? udpEndPoint;
    private GeneralLogger logger;
    private readonly IUdpConnectionFactory connectionFactory;
    private readonly UdpMetrics? metrics;
    private Socket? listenSocket;
    private string localEndPointString;

    public UdpConnectionListener(EndPoint? udpEndPoint, GeneralLogger logger, IUdpConnectionFactory connectionFactory, IServiceProvider serviceProvider)
    {
        this.udpEndPoint = udpEndPoint;
        this.logger = logger;
        this.connectionFactory = connectionFactory;
        this.metrics = serviceProvider.GetService<UdpMetrics>();
    }

    public EndPoint EndPoint => udpEndPoint;

    internal void Bind()
    {
        if (this.listenSocket != null)
        {
            throw new InvalidOperationException("Transport is already bound.");
        }

        Socket listenSocket;
        try
        {
            listenSocket = CreateBoundListenSocket(EndPoint);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            throw new AddressInUseException(e.Message, e);
        }

        Debug.Assert(listenSocket.LocalEndPoint != null);

        this.listenSocket = listenSocket;
        udpEndPoint = listenSocket.LocalEndPoint;
        localEndPointString = udpEndPoint.ToString().Reverse();
    }

    private Socket CreateBoundListenSocket(EndPoint endPoint)
    {
        if (endPoint is IPEndPoint ip)
        {
            var r = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            r.Bind(endPoint);

            return r;
        }

        return null;
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                Debug.Assert(listenSocket != null, "Bind must be called first.");
                var r = await connectionFactory.ReceiveAsync(listenSocket, cancellationToken);
                metrics?.RecordUdpReceiveBytes(r.ReceivedBytesCount);
                return new UdpConnectionContext(listenSocket, r) { LocalEndPointString = localEndPointString };
            }
            catch (ObjectDisposedException)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException)
            {
                // The connection got reset while it was in the backlog, so we try again.
                logger.ConnectionReset("(null)");
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        listenSocket?.Dispose();

        return default;
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        listenSocket?.Dispose();
        return default;
    }
}