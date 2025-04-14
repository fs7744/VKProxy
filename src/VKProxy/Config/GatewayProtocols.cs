namespace VKProxy.Config;

[Flags]
public enum GatewayProtocols
{
    TCP = 1,
    UDP = 2,
    HTTP1 = 4,
    HTTP2 = 8,
    HTTP3 = 16,
}