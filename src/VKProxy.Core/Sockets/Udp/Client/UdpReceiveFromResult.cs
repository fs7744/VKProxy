using System.Buffers;
using System.Net;

namespace VKProxy.Core.Sockets.Udp.Client;

public struct UdpReceiveFromResult
{
    public int ReceivedBytesCount;
    public EndPoint RemoteEndPoint;
    public IMemoryOwner<byte> Buffer;

    public ReadOnlyMemory<byte> GetReceivedBytes() => Buffer.Memory.Slice(0, ReceivedBytesCount);
}