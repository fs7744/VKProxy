using System.Net;

namespace VKProxy.Middlewares.Socks5;

public sealed record Socks5CmdRequest
{
    public Socks5Cmd Cmd { get; set; }
    public Socks5Address AddressType { get; set; }
    public ushort Port { get; set; }
    public IPAddress Ip { get; set; }
    public string Domain { get; internal set; }
}