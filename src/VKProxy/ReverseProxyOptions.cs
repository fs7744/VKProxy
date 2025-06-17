using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using VKProxy.Core.Config;
using VKProxy.Features.Limits;

namespace VKProxy;

public class ReverseProxyOptions
{
    public string Section { get; set; } = "ReverseProxy";
    public int RouteCahceSize { get; set; } = 1024;
    public TimeSpan DefaultProxyTimeout { get; set; } = TimeSpan.FromSeconds(300);

    public TimeSpan? DnsRefreshPeriod { get; set; } = TimeSpan.FromMinutes(5);
    public AddressFamily? DnsAddressFamily { get; set; }
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(3);
    public StringComparison RouteComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    public ConcurrentConnectionLimitOptions Limit { get; set; }
}

internal class ReverseProxyOptionsSetup : IConfigureOptions<ReverseProxyOptions>
{
    private readonly IConfiguration configuration;

    public ReverseProxyOptionsSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(ReverseProxyOptions options)
    {
        var section = configuration.GetSection(options.Section);
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(ReverseProxyOptions.RouteCahceSize));
        if (i.HasValue) options.RouteCahceSize = i.Value;

        var t = section.ReadTimeSpan(nameof(ReverseProxyOptions.DefaultProxyTimeout));
        if (t.HasValue) options.DefaultProxyTimeout = t.Value;

        t = section.ReadTimeSpan(nameof(ReverseProxyOptions.DnsRefreshPeriod));
        if (t.HasValue) options.DnsRefreshPeriod = t.Value;

        t = section.ReadTimeSpan(nameof(ReverseProxyOptions.ConnectionTimeout));
        if (t.HasValue) options.ConnectionTimeout = t.Value;

        var tt = section.ReadEnum<AddressFamily>(nameof(ReverseProxyOptions.DnsAddressFamily));
        if (tt.HasValue) options.DnsAddressFamily = tt.Value;

        var r = section.ReadEnum<StringComparison>(nameof(ReverseProxyOptions.RouteComparison));
        if (r.HasValue) options.RouteComparison = r.Value;

        options.Limit = ConcurrentConnectionLimitOptions.Read(section.GetSection(nameof(ReverseProxyOptions.Limit)));
    }
}