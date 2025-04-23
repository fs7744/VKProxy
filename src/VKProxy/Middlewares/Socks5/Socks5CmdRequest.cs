using System.Net;

namespace VKProxy.Middlewares.Socks5;

public sealed record Socks5CmdRequest : Socks5Common
{
    public Socks5Cmd Cmd { get; set; }
}

public record Socks5Common
{
    public Socks5Address AddressType { get; set; }
    public ushort Port { get; set; }
    public IPAddress Ip { get; set; }
    public string Domain { get; set; }
}

public sealed record Socks5Udp : Socks5Common
{
    public ReadOnlyMemory<byte> Data { get; set; }
}