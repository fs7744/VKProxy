using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VKProxy.Core.Config;

namespace VKProxy.Config;

internal class NamedPipeTransportOptionsSetup : IConfigureOptions<NamedPipeTransportOptions>
{
    private readonly IConfiguration configuration;

    public NamedPipeTransportOptionsSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(NamedPipeTransportOptions options)
    {
        var section = configuration.GetSection("ServerOptions");
        if (!section.Exists()) return;

        section = section.GetSection("NamedPipe");
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(NamedPipeTransportOptions.ListenerQueueCount));
        if (i.HasValue) options.ListenerQueueCount = i.Value;

        var b = section.ReadBool(nameof(NamedPipeTransportOptions.CurrentUserOnly));
        if (b.HasValue) options.CurrentUserOnly = b.Value;

        var l = section.ReadInt64(nameof(NamedPipeTransportOptions.MaxReadBufferSize));
        if (l.HasValue) options.MaxReadBufferSize = l.Value;

        l = section.ReadInt64(nameof(NamedPipeTransportOptions.MaxWriteBufferSize));
        if (l.HasValue) options.MaxWriteBufferSize = l.Value;
    }
}