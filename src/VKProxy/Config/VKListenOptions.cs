using VKProxy.Core.Config;

namespace VKProxy.Config;

public class VKListenOptions : EndPointOptions
{
    public GatewayProtocols Protocols { get; set; }
}