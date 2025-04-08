namespace VKProxy.Config;

[Flags]
public enum GatewayProtocols
{
    TCP = 1,
    UDP = 2,
    SNI = 4,
    HTTP1 = 8,
    HTTP2 = 16,
    HTTP3 = 32,
}