using Microsoft.AspNetCore.Connections;
using System.Net;
using System.Net.Sockets;
using VKProxy.Core.Sockets.Udp;

namespace VKProxy.Config.Validators;

public class CommonEndPointConvertor : IEndPointConvertor
{
    private const string UnixPipeHostPrefix = "unix:/";
    private const string NamedPipeHostPrefix = "pipe:/";

    public bool TryConvert(string address, GatewayProtocols protocols, out IEnumerable<EndPoint> endPoint)
    {
        if (IPEndPoint.TryParse(address, out var ip))
        {
            endPoint = [protocols == GatewayProtocols.UDP ? new UdpEndPoint(ip.Address, ip.Port) : ip];
            return true;
        }
        else if (address.StartsWith(UnixPipeHostPrefix, StringComparison.OrdinalIgnoreCase))
        {
            endPoint = [new UnixDomainSocketEndPoint(GetUnixPipePath(address))];
            return true;
        }
        else if (address.StartsWith(NamedPipeHostPrefix, StringComparison.OrdinalIgnoreCase))
        {
            endPoint = [new NamedPipeEndPoint(address.Substring(NamedPipeHostPrefix.Length))];
            return true;
        }
        else if (address.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(address.AsSpan(10), out var port)
            && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
        {
            endPoint = protocols == GatewayProtocols.UDP
                ? [new UdpEndPoint(IPAddress.Loopback, port), new UdpEndPoint(IPAddress.IPv6Loopback, port)]
                : [new IPEndPoint(IPAddress.Loopback, port), new IPEndPoint(IPAddress.IPv6Loopback, port)];
            return true;
        }
        else if (address.StartsWith("*:")
            && int.TryParse(address.AsSpan(2), out port)
            && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
        {
            endPoint = protocols == GatewayProtocols.UDP
                ? [new UdpEndPoint(IPAddress.Any, port), new UdpEndPoint(IPAddress.IPv6Any, port)]
                : [new IPEndPoint(IPAddress.Any, port), new IPEndPoint(IPAddress.IPv6Any, port)];
            return true;
        }

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