using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VKProxy.Core.Config;

namespace VKProxy.Config;

internal class QuicTransportOptionsSetup : IConfigureOptions<QuicTransportOptions>
{
    private readonly IConfiguration configuration;

    public QuicTransportOptionsSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(QuicTransportOptions options)
    {
        var section = configuration.GetSection("ServerOptions");
        if (!section.Exists()) return;

        section = section.GetSection("Quic");
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(QuicTransportOptions.MaxBidirectionalStreamCount));
        if (i.HasValue) options.MaxBidirectionalStreamCount = i.Value;

        i = section.ReadInt32(nameof(QuicTransportOptions.Backlog));
        if (i.HasValue) options.Backlog = i.Value;

        i = section.ReadInt32(nameof(QuicTransportOptions.MaxUnidirectionalStreamCount));
        if (i.HasValue) options.MaxUnidirectionalStreamCount = i.Value;

        var l = section.ReadInt64(nameof(QuicTransportOptions.MaxReadBufferSize));
        if (l.HasValue) options.MaxReadBufferSize = l.Value;

        l = section.ReadInt64(nameof(QuicTransportOptions.MaxWriteBufferSize));
        if (l.HasValue) options.MaxWriteBufferSize = l.Value;

        l = section.ReadInt64(nameof(QuicTransportOptions.DefaultStreamErrorCode));
        if (l.HasValue) options.DefaultStreamErrorCode = l.Value;

        l = section.ReadInt64(nameof(QuicTransportOptions.DefaultCloseErrorCode));
        if (l.HasValue) options.DefaultCloseErrorCode = l.Value;
    }
}
