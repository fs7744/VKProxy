using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly UdpMetrics? metrics;
    private readonly UdpReceiverPool socketReceiverPool;

    public UdpConnectionFactory(IOptions<SocketTransportOptions> options, IOptions<UdpSocketTransportOptions> udpOptions, IMemoryPoolSizeFactory<byte> factory, IServiceProvider provider)
    {
        var op = udpOptions.Value;
        pool = op.UdpMaxSize == 4096 ? factory.Create() : factory.Create(op.UdpMaxSize);

        pipeScheduler = options.Value.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;
        socketReceiverPool = new UdpReceiverPool(pipeScheduler, op.UdpPoolSize);
        socketSenderPool = new UdpSenderPool(OperatingSystem.IsWindows() ? pipeScheduler : PipeScheduler.Inline, op.UdpPoolSize);
        this.metrics = provider.GetService<UdpMetrics>();
    }

    public async ValueTask<UdpReceiveFromResult> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
    {
        var receiver = socketReceiverPool.Rent();
        try
        {
            var buffer = pool.Rent();
            receiver.RemoteEndPoint = socket.LocalEndPoint;
            var r = await receiver.ReceiveFromAsync(socket, buffer.Memory);
            metrics?.RecordClientUdpReceiveBytes(r.ReceivedBytes);
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
            metrics?.RecordClientUdpSentBytes(receivedBytes.Length);
            return await sender.SendToAsync(socket, receivedBytes);
        }
        finally
        {
            socketSenderPool.Return(sender);
        }
    }
}