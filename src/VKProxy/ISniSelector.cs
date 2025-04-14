using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using VKProxy.Config;
using VKProxy.Core.Routing;
using DotNext;

namespace VKProxy;

public interface ISniSelector
{
    Task ReBuildAsync(IReadOnlyDictionary<string, SniConfig> sni, CancellationToken cancellationToken);

    X509Certificate2? ServerCertificateSelector(ConnectionContext? context, string? host);
}

public class SniSelector : ISniSelector
{
    private readonly ReverseProxyOptions options;
    private IRouteTable<X509Certificate2> route;

    public SniSelector(IOptions<ReverseProxyOptions> options)
    {
        this.options = options.Value;
    }

    public async Task ReBuildAsync(IReadOnlyDictionary<string, SniConfig> sni, CancellationToken cancellationToken)
    {
        var sniRouteBuilder = new RouteTableBuilder<X509Certificate2>(StringComparison.OrdinalIgnoreCase, options.SniRouteCahceSize);
        foreach (var route in sni.Values.Where(i => i.Certificate != null))
        {
            foreach (var host in route.Host)
            {
                if (host.StartsWith("localhost:"))
                {
                    Set(sniRouteBuilder, route, $"127.0.0.1:{host.AsSpan(10)}");
                    Set(sniRouteBuilder, route, $"[::1]:{host.AsSpan(10)}");
                }
                Set(sniRouteBuilder, route, host);
            }
        }

        route = sniRouteBuilder.Build(RouteTableType.OnlyFirst);

        static void Set(RouteTableBuilder<X509Certificate2> builder, SniConfig? route, string host)
        {
            if (host.StartsWith('*'))
            {
                builder.Add(host[1..].Reverse(), route.Certificate, RouteType.Prefix, route.Order);
            }
            else
            {
                builder.Add(host.Reverse(), route.Certificate, RouteType.Exact, route.Order);
            }
        }
    }

    public X509Certificate2? ServerCertificateSelector(ConnectionContext? context, string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        var s = route.Match<SniConfig>(host.Reverse(), null, static (c, r) => true);
        return s;
    }
}