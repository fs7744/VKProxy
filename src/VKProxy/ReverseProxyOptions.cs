using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using VKProxy.Core.Config;

namespace VKProxy;

public class ReverseProxyOptions
{
    public int SniRouteCahceSize { get; set; } = 1024;
    public int HttpRouteCahceSize { get; set; } = 1024;
    public TimeSpan DefaultProxyTimeout { get; set; } = TimeSpan.FromSeconds(300);

    public TimeSpan? DnsRefreshPeriod { get; set; } = TimeSpan.FromMinutes(5);
    public AddressFamily? DnsAddressFamily { get; set; }
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(3);
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
        var section = configuration.GetSection("ReverseProxy");
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(ReverseProxyOptions.SniRouteCahceSize));
        if (i.HasValue) options.SniRouteCahceSize = i.Value;

        i = section.ReadInt32(nameof(ReverseProxyOptions.HttpRouteCahceSize));
        if (i.HasValue) options.HttpRouteCahceSize = i.Value;

        var t = section.ReadTimeSpan(nameof(ReverseProxyOptions.DefaultProxyTimeout));
        if (t.HasValue) options.DefaultProxyTimeout = t.Value;

        t = section.ReadTimeSpan(nameof(ReverseProxyOptions.DnsRefreshPeriod));
        if (t.HasValue) options.DnsRefreshPeriod = t.Value;

        t = section.ReadTimeSpan(nameof(ReverseProxyOptions.ConnectionTimeout));
        if (t.HasValue) options.ConnectionTimeout = t.Value;

        var tt = section.ReadEnum<AddressFamily>(nameof(ReverseProxyOptions.DnsAddressFamily));
        if (tt.HasValue) options.DnsAddressFamily = tt.Value;
    }
}