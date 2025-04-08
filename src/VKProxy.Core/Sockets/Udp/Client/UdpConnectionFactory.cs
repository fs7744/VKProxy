using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using VKProxy.Core.Buffers;

namespace VKProxy.Core.Sockets.Udp.Client;

public class UdpConnectionFactory : IUdpConnectionFactory
{
    private readonly MemoryPool<byte> pool;
    private readonly PipeScheduler pipeScheduler;
    private readonly UdpSenderPool socketSenderPool;
    private readonly UdpReceiverPool socketReceiverPool;

    public UdpConnectionFactory(IOptions<SocketTransportOptions> options, IOptions<UdpSocketTransportOptions> udpOptions)
    {
        var op = udpOptions.Value;
        pool = PinnedBlockMemoryPoolFactory.Create(op.UdpMaxSize);

        pipeScheduler = options.Value.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;
        socketReceiverPool = new UdpReceiverPool(pipeScheduler, op.UdpPoolSize);
        socketSenderPool = new UdpSenderPool(OperatingSystem.IsWindows() ? pipeScheduler : PipeScheduler.Inline, op.UdpPoolSize);
    }

    public async ValueTask<UdpReceiveFromResult> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
    {
        var receiver = socketReceiverPool.Rent();
        try
        {
            var buffer = pool.Rent();
            receiver.RemoteEndPoint = socket.LocalEndPoint;
            var r = await receiver.ReceiveFromAsync(socket, buffer.Memory);
            return new UdpReceiveFromResult { RemoteEndPoint = r.RemoteEndPoint, ReceivedBytesCount = r.ReceivedBytes, Buffer = buffer };
        }
        finally
        {
            socketReceiverPool.Return(receiver);
        }
    }

    public async Task<int> SendToAsync(Socket socket, EndPoint remoteEndPoint, ReadOnlyMemory<byte> receivedBytes, CancellationToken cancellationToken)
    {
        var sender = socketSenderPool.Rent();
        sender.RemoteEndPoint = remoteEndPoint;
        sender.SocketFlags = SocketFlags.None;
        try
        {
            return await sender.SendToAsync(socket, receivedBytes);
        }
        finally
        {
            socketSenderPool.Return(sender);
        }
    }
}