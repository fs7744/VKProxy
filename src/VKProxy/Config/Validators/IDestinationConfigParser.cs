using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Sockets;

namespace VKProxy.Config.Validators;

public interface IDestinationConfigParser
{
    bool TryParse(DestinationConfig config, out DestinationState state);
}

public class DestinationConfigParser : IDestinationConfigParser
{
    public bool TryParse(DestinationConfig config, out DestinationState state)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.Address))
        {
            state = null;
            return false;
        }
        var address = config.Address;
        if (IPEndPoint.TryParse(address, out var ip))
        {
            state = new DestinationState() { EndPoint = ip, Host = config.Host, Address = $"http://{address}" };
            return true;
        }
        if (address.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal) < 0)
        {
            state = null;
            return false;
        }

        var d = BindingAddress.Parse(address);
        if (d.IsUnixPipe)
        {
            state = new DestinationState() { EndPoint = new UnixDomainSocketEndPoint(d.UnixPipePath), Host = config.Host, Address = address };
            return true;
        }
        else if (d.IsNamedPipe)
        {
            state = new DestinationState() { EndPoint = new NamedPipeEndPoint(d.NamedPipeName), Host = config.Host, Address = address };
            return true;
        }
        else if (IPAddress.TryParse(d.Host, out var ipe))
        {
            state = new DestinationState() { EndPoint = new IPEndPoint(ipe, d.Port), Host = config.Host, Address = address };
            return true;
        }
        else
        {
            state = null;
            return false;
        }
    }
}