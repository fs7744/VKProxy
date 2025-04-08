namespace VKProxy.Core.Sockets.Udp;

public class UdpSocketTransportOptions
{
    public int UdpMaxSize { get; set; } = 4096;

    public int UdpPoolSize { get; set; } = 1024;
}