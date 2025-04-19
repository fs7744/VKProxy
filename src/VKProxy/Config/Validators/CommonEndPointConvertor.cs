using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Sockets;
using VKProxy.Core.Sockets.Udp;

namespace VKProxy.Config.Validators;

public class CommonEndPointConvertor : IEndPointConvertor
{
    public bool TryConvert(string address, GatewayProtocols protocols, out EndPoint endPoint)
    {
        if (address == null)
        {
            endPoint = null;
            return false;
        }
        if (IPEndPoint.TryParse(address, out var ip))
        {
            endPoint = protocols == GatewayProtocols.UDP ? new UdpEndPoint(ip.Address, ip.Port) : ip;
            return true;
        }
        if (address.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal) < 0)
        {
            endPoint = null;
            return false;
        }

        var d = BindingAddress.Parse(address);
        if (d.IsUnixPipe)
        {
            endPoint = new UnixDomainSocketEndPoint(d.UnixPipePath);
            return true;
        }
        else if (d.IsNamedPipe)
        {
            endPoint = new NamedPipeEndPoint(d.NamedPipeName);
            return true;
        }
        else if (IPAddress.TryParse(d.Host, out var ipe))
        {
            endPoint = protocols == GatewayProtocols.UDP ? new UdpEndPoint(ipe, d.Port) : new IPEndPoint(ipe, d.Port);
            return true;
        }

        endPoint = null;
        return false;
    }
}