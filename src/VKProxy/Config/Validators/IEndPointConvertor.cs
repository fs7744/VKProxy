using System.Net;

namespace VKProxy.Config.Validators;

public interface IEndPointConvertor
{
    public bool TryConvert(string address, GatewayProtocols protocols, out IEnumerable<EndPoint> endPoint);
}