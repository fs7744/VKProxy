using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;
using VKProxy.Core.Sockets.Udp;
using VKProxy.Core.Sockets.Udp.Client;

namespace CoreDemo;

internal class UdpListenHandler : ListenHandlerBase
{
    private readonly ILogger<UdpListenHandler> logger;
    private readonly IUdpConnectionFactory udp;
    private readonly IPEndPoint proxyServer = new(IPAddress.Parse("127.0.0.1"), 11000);

    public UdpListenHandler(ILogger<UdpListenHandler> logger, IUdpConnectionFactory udp)
    {
        this.logger = logger;
        this.udp = udp;
    }

    public override async Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        var ip = new EndPointOptions()
        {
            EndPoint = UdpEndPoint.Parse("127.0.0.1:5000"),
            Key = "tcp"
        };
        await transportManager.BindAsync(ip, Proxy, cancellationToken);
        logger.LogInformation($"listen {ip.EndPoint}");
    }

    private async Task Proxy(ConnectionContext connection)
    {
        if (connection is UdpConnectionContext context)
        {
            Console.WriteLine($"{context.LocalEndPoint} received {context.ReceivedBytesCount} from {context.RemoteEndPoint}");
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            await udp.SendToAsync(socket, proxyServer, context.ReceivedBytes, CancellationToken.None);
            var r = await udp.ReceiveAsync(socket, CancellationToken.None);
            await udp.SendToAsync(context.Socket, context.RemoteEndPoint, r.GetReceivedBytes(), CancellationToken.None);
        }
    }
}