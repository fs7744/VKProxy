using System.Net;

namespace VKProxy.Core.Sockets.Udp;

public class UdpEndPoint : IPEndPoint
{
    public UdpEndPoint(long address, int port) : base(address, port)
    {
    }

    public UdpEndPoint(IPAddress address, int port) : base(address, port)
    {
    }

    public new static UdpEndPoint Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        var ip = Parse(s.AsSpan());
        return new UdpEndPoint(ip.Address, ip.Port);
    }
}