using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Sockets;
using VKProxy.Core.Sockets.Udp;

namespace VKProxy.Config.Validators;

public class CommonEndPointConvertor : IEndPointConvertor
{
    internal const string UnixPipeHostPrefix = "unix:/";
    internal const string NamedPipeHostPrefix = "pipe:/";

    public bool TryConvert(string address, GatewayProtocols protocols, out IEnumerable<EndPoint> endPoint)
    {
        if (IPEndPoint.TryParse(address, out var ip))
        {
            endPoint = [protocols == GatewayProtocols.UDP ? new UdpEndPoint(ip.Address, ip.Port) : ip];
            return true;
        }
        else if (address.StartsWith("*:")
            && int.TryParse(address.AsSpan(2), out var port)
            && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
        {
            endPoint = protocols == GatewayProtocols.UDP
                ? [new UdpEndPoint(IPAddress.Any, port), new UdpEndPoint(IPAddress.IPv6Any, port)]
                : [new IPEndPoint(IPAddress.Any, port), new IPEndPoint(IPAddress.IPv6Any, port)];
            return true;
        }
        else if (address.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(address.AsSpan(10), out port)
            && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
        {
            endPoint = protocols == GatewayProtocols.UDP
                ? [new UdpEndPoint(IPAddress.Loopback, port), new UdpEndPoint(IPAddress.IPv6Loopback, port)]
                : [new IPEndPoint(IPAddress.Loopback, port), new IPEndPoint(IPAddress.IPv6Loopback, port)];
            return true;
        }
        //todo
        var d = BindingAddress.Parse(address);
        if (d.IsUnixPipe)
        {
            endPoint = [new UnixDomainSocketEndPoint(d.UnixPipePath)];
            return true;
        }
        else if (d.IsNamedPipe)
        {
            endPoint = [new NamedPipeEndPoint(d.NamedPipeName)];
            return true;
        }
        else if (IPAddress.TryParse(d.Host, out var ipe))
        {
            endPoint = [protocols == GatewayProtocols.UDP ? new UdpEndPoint(ipe, d.Port) : new IPEndPoint(ipe, d.Port)];
            return true;
        }

        //if (IPEndPoint.TryParse(address, out var ip))
        //{
        //    endPoint = [protocols == GatewayProtocols.UDP ? new UdpEndPoint(ip.Address, ip.Port) : ip];
        //    return true;
        //}
        //else if (address.StartsWith(UnixPipeHostPrefix, StringComparison.OrdinalIgnoreCase))
        //{
        //    endPoint = [new UnixDomainSocketEndPoint(GetUnixPipePath(address))];
        //    return true;
        //}
        //else if (address.StartsWith(NamedPipeHostPrefix, StringComparison.OrdinalIgnoreCase))
        //{
        //    endPoint = [new NamedPipeEndPoint(address.Substring(NamedPipeHostPrefix.Length))];
        //    return true;
        //}
        //else if (address.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase)
        //    && int.TryParse(address.AsSpan(10), out var port)
        //    && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
        //{
        //    endPoint = protocols == GatewayProtocols.UDP
        //        ? [new UdpEndPoint(IPAddress.Loopback, port), new UdpEndPoint(IPAddress.IPv6Loopback, port)]
        //        : [new IPEndPoint(IPAddress.Loopback, port), new IPEndPoint(IPAddress.IPv6Loopback, port)];
        //    return true;
        //}

        endPoint = null;
        return false;
    }

    private static string GetUnixPipePath(string host)
    {
        var unixPipeHostPrefixLength = UnixPipeHostPrefix.Length;
        if (!OperatingSystem.IsWindows())
        {
            // "/" character in unix refers to root. Windows has drive letters and volume separator (c:)
            unixPipeHostPrefixLength--;
        }
        return host.Substring(unixPipeHostPrefixLength);
    }
}