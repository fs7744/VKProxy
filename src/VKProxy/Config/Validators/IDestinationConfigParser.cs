using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Sockets;

namespace VKProxy.Config.Validators;

public interface IDestinationConfigParser
{
    bool CanParse(DestinationConfig config);

    IEnumerable<DestinationState> Parse(DestinationConfig config);
}

public class DestinationConfigParser : IDestinationConfigParser
{
    public bool CanParse(DestinationConfig config)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.Address)) return false;
        var address = config.Address;
        if (IPEndPoint.TryParse(address, out _))
        {
            return true;
        }
        else if (address.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(address.AsSpan(10), out var port)
            && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
        {
            return true;
        }
        else if (address.StartsWith(CommonEndPointConvertor.UnixPipeHostPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        else if (address.StartsWith(CommonEndPointConvertor.NamedPipeHostPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public IEnumerable<DestinationState> Parse(DestinationConfig config)
    {
        throw new NotImplementedException();
    }
}