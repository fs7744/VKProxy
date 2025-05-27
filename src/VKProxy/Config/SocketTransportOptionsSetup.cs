using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VKProxy.Core.Config;

namespace VKProxy.Config;

internal class SocketTransportOptionsSetup : IConfigureOptions<SocketTransportOptions>
{
    private readonly IConfiguration configuration;

    public SocketTransportOptionsSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(SocketTransportOptions options)
    {
        var section = configuration.GetSection("ServerOptions");
        if (!section.Exists()) return;

        section = section.GetSection("Socket");
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(SocketTransportOptions.IOQueueCount));
        if (i.HasValue) options.IOQueueCount = i.Value;

        i = section.ReadInt32(nameof(SocketTransportOptions.Backlog));
        if (i.HasValue) options.Backlog = i.Value;

        var l = section.ReadInt64(nameof(SocketTransportOptions.MaxReadBufferSize));
        if (l.HasValue) options.MaxReadBufferSize = l.Value;

        l = section.ReadInt64(nameof(SocketTransportOptions.MaxWriteBufferSize));
        if (l.HasValue) options.MaxWriteBufferSize = l.Value;

        var b = section.ReadBool(nameof(SocketTransportOptions.WaitForDataBeforeAllocatingBuffer));
        if (b.HasValue) options.WaitForDataBeforeAllocatingBuffer = b.Value;

        b = section.ReadBool(nameof(SocketTransportOptions.UnsafePreferInlineScheduling));
        if (b.HasValue) options.UnsafePreferInlineScheduling = b.Value;

        b = section.ReadBool(nameof(SocketTransportOptions.NoDelay));
        if (b.HasValue) options.NoDelay = b.Value;
    }
}