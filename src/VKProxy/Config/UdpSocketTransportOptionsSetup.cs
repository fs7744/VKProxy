using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VKProxy.Core.Config;
using VKProxy.Core.Sockets.Udp;

namespace VKProxy.Config;

internal class UdpSocketTransportOptionsSetup : IConfigureOptions<UdpSocketTransportOptions>
{
    private readonly IConfiguration configuration;

    public UdpSocketTransportOptionsSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(UdpSocketTransportOptions options)
    {
        var section = configuration.GetSection("ServerOptions");
        if (!section.Exists()) return;

        section = section.GetSection("Socket");
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(UdpSocketTransportOptions.UdpMaxSize));
        if (i.HasValue) options.UdpMaxSize = i.Value;

        i = section.ReadInt32(nameof(UdpSocketTransportOptions.UdpPoolSize));
        if (i.HasValue) options.UdpPoolSize = i.Value;
    }
}