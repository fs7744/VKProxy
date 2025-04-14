using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VKProxy.Core.Config;

namespace VKProxy;

public class ReverseProxyOptions
{
    public int SniRouteCahceSize { get; set; } = 100;
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

        section = section.GetSection("NamedPipe");
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(ReverseProxyOptions.SniRouteCahceSize));
        if (i.HasValue) options.SniRouteCahceSize = i.Value;
    }
}