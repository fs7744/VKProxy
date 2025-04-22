namespace VKProxy.Middlewares.Socks5;

public enum Socks5Cmd : byte
{
    Connect = 1,
    Bind = 2,
    UdpAssociate = 3
}
