using DotNext;
using DotNext.Collections.Generic;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using System;
using System.IO.Pipelines;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using VKProxy.Config;
using VKProxy.Core.Buffers;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Core.Routing;
using VKProxy.Core.Tls;

namespace VKProxy;

public interface ISniSelector
{
    ValueTask<(SniConfig sni, ReadResult result)> MatchSNIAsync(ConnectionContext context, CancellationToken token);

    Task ReBuildAsync(IReadOnlyDictionary<string, SniConfig> sni, CancellationToken cancellationToken);

    X509Certificate2? ServerCertificateSelector(ConnectionContext? context, string? host);
}

public class SniSelector : ISniSelector
{
    private readonly ReverseProxyOptions options;
    private readonly ProxyLogger logger;
    private IRouteTable<SniConfig> route;
    private Dictionary<string, string> hosts;

    public SniSelector(IOptions<ReverseProxyOptions> options, ProxyLogger logger)
    {
        this.options = options.Value;
        this.logger = logger;
        hosts = new Dictionary<string, string>(CollectionUtilities.MatchComparison(this.options.RouteComparison));
    }

    public Task ReBuildAsync(IReadOnlyDictionary<string, SniConfig> sni, CancellationToken cancellationToken)
    {
        var sniRouteBuilder = new RouteTableBuilder<SniConfig>(options.RouteComparison, options.RouteCahceSize);
        foreach (var route in sni.Values.Where(i => i.Passthrough || i.Certificate != null))
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
        var old = route;
        route = sniRouteBuilder.Build(RouteTableType.OnlyFirst);

        old?.Dispose();

        return Task.CompletedTask;

        static void Set(RouteTableBuilder<SniConfig> builder, SniConfig? route, string host)
        {
            if (host.StartsWith('*'))
            {
                builder.Add(host[1..].Reverse(), route, RouteType.Prefix, route.Order);
            }
            else
            {
                builder.Add(host.Reverse(), route, RouteType.Exact, route.Order);
            }
        }
    }

    public X509Certificate2? ServerCertificateSelector(ConnectionContext? context, string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        var s = route.Match<SniConfig>(hosts.GetOrAdd(host, static host => host.Reverse()), null, static (c, r) => true);
        return s?.X509Certificate2;
    }

    public async ValueTask<(SniConfig sni, ReadResult result)> MatchSNIAsync(ConnectionContext context, CancellationToken token)
    {
        var (hello, rr) = await TryGetClientHelloAsync(context, token);
        if (hello.HasValue)
        {
            var h = hello.Value;
            var r = await route.MatchAsync<SniConfig>(hosts.GetOrAdd(h.TargetName, static host => host.Reverse()), null, static (i, j) => true);
            if (r is null || !MatchSNI(r, h))
            {
                logger.NotFoundRouteSni(h.TargetName);
                return (null, rr);
            }
            return (r, rr);
        }
        else
        {
            logger.NotFoundRouteSni("client hello failed");
            return (null, rr);
        }
    }

    private bool MatchSNI(SniConfig config, TlsFrameInfo info)
    {
        if (config.Passthrough) return true;
        if (config.Certificate is null) return false;
        var vv = config.Protocols;
        if (!vv.HasValue) return true;
        var v = vv.Value;
        if (v == SslProtocols.None) return true;
        var t = info.SupportedVersions;
        if (v.HasFlag(SslProtocols.Tls13) && t.HasFlag(SslProtocols.Tls13)) return true;
        else if (v.HasFlag(SslProtocols.Tls12) && t.HasFlag(SslProtocols.Tls12)) return true;
        else if (v.HasFlag(SslProtocols.Tls11) && t.HasFlag(SslProtocols.Tls11)) return true;
        else if (v.HasFlag(SslProtocols.Tls) && t.HasFlag(SslProtocols.Tls)) return true;
        else if (v.HasFlag(SslProtocols.Ssl3) && t.HasFlag(SslProtocols.Ssl3)) return true;
        else if (v.HasFlag(SslProtocols.Ssl2) && t.HasFlag(SslProtocols.Ssl2)) return true;
        else if (v.HasFlag(SslProtocols.Default) && t.HasFlag(SslProtocols.Default)) return true;
        else return false;
    }

    private static async ValueTask<(TlsFrameInfo?, ReadResult)> TryGetClientHelloAsync(ConnectionContext context, CancellationToken token)
    {
        var input = context.Transport.Input;
        TlsFrameInfo info = default;
        while (true)
        {
            var f = await input.ReadAsync(token).ConfigureAwait(false);
            if (f.IsCompleted)
            {
                return (null, f);
            }
            var buffer = f.Buffer;
            if (buffer.Length == 0)
            {
                continue;
            }

            var data = buffer.ToSpan();
            if (TlsFrameHelper.TryGetFrameInfo(data, ref info))
            {
                return (info, f);
            }
            else
            {
                input.AdvanceTo(buffer.Start, buffer.End);
                continue;
            }
        }
    }
}