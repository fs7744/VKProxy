using System.Net;

namespace VKProxy.Config.Validators;

public interface IEndPointConvertor
{
    public bool TryConvert(string address, GatewayProtocols protocols, out EndPoint endPoint);
}