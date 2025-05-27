using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VKProxy.Core.Config;

namespace VKProxy.Config;

internal class KestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
{
    private readonly IConfiguration configuration;

    public KestrelServerOptionsSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(KestrelServerOptions options)
    {
        options.AddServerHeader = false;
        options.AllowHostHeaderOverride = false;
        var section = configuration.GetSection("ServerOptions");
        if (!section.Exists()) return;

        var b = section.ReadBool(nameof(KestrelServerOptions.AllowHostHeaderOverride));
        if (b.HasValue) options.AllowHostHeaderOverride = b.Value;

        b = section.ReadBool(nameof(KestrelServerOptions.DisableStringReuse));
        if (b.HasValue) options.DisableStringReuse = b.Value;

        b = section.ReadBool(nameof(KestrelServerOptions.AllowAlternateSchemes));
        if (b.HasValue) options.AllowAlternateSchemes = b.Value;

        b = section.ReadBool(nameof(KestrelServerOptions.AllowSynchronousIO));
        if (b.HasValue) options.AllowSynchronousIO = b.Value;

        b = section.ReadBool(nameof(KestrelServerOptions.AllowResponseHeaderCompression));
        if (b.HasValue) options.AllowResponseHeaderCompression = b.Value;

        b = section.ReadBool(nameof(KestrelServerOptions.AddServerHeader));
        if (b.HasValue) options.AddServerHeader = b.Value;

        ConfigureLimits(options.Limits, section.GetSection(nameof(KestrelServerOptions.Limits)));
    }

    private void ConfigureLimits(KestrelServerLimits limits, IConfigurationSection section)
    {
        if (!section.Exists()) return;

        var l = section.ReadInt64(nameof(KestrelServerLimits.MaxResponseBufferSize));
        if (l.HasValue) limits.MaxResponseBufferSize = l.Value;

        l = section.ReadInt64(nameof(KestrelServerLimits.MaxRequestBufferSize));
        if (l.HasValue) limits.MaxRequestBufferSize = l.Value;

        l = section.ReadInt64(nameof(KestrelServerLimits.MaxRequestBodySize));
        if (l.HasValue) limits.MaxRequestBodySize = l.Value;

        l = section.ReadInt64(nameof(KestrelServerLimits.MaxConcurrentConnections));
        if (l.HasValue) limits.MaxConcurrentConnections = l.Value;

        l = section.ReadInt64(nameof(KestrelServerLimits.MaxConcurrentUpgradedConnections));
        if (l.HasValue) limits.MaxConcurrentUpgradedConnections = l.Value;

        var i = section.ReadInt32(nameof(KestrelServerLimits.MaxRequestLineSize));
        if (i.HasValue) limits.MaxRequestLineSize = i.Value;

        i = section.ReadInt32(nameof(KestrelServerLimits.MaxRequestHeadersTotalSize));
        if (i.HasValue) limits.MaxRequestHeadersTotalSize = i.Value;

        i = section.ReadInt32(nameof(KestrelServerLimits.MaxRequestHeaderCount));
        if (i.HasValue) limits.MaxRequestHeaderCount = i.Value;

        var t = section.ReadTimeSpan(nameof(KestrelServerLimits.KeepAliveTimeout));
        if (t.HasValue) limits.KeepAliveTimeout = t.Value;

        t = section.ReadTimeSpan(nameof(KestrelServerLimits.RequestHeadersTimeout));
        if (t.HasValue) limits.RequestHeadersTimeout = t.Value;

        var s = CreateMinDataRate(section.GetSection(nameof(KestrelServerLimits.MinRequestBodyDataRate)));
        if (s != null)
        {
            limits.MinRequestBodyDataRate = s;
        }
        s = CreateMinDataRate(section.GetSection(nameof(KestrelServerLimits.MinResponseDataRate)));
        if (s != null)
        {
            limits.MinResponseDataRate = s;
        }

        ConfigureHttp2Limits(limits.Http2, section.GetSection(nameof(KestrelServerLimits.Http2)));
        ConfigureHttp3Limits(limits.Http3, section.GetSection(nameof(KestrelServerLimits.Http3)));
    }

    private void ConfigureHttp2Limits(Http2Limits limits, IConfigurationSection section)
    {
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(Http2Limits.HeaderTableSize));
        if (i.HasValue) limits.HeaderTableSize = i.Value;

        i = section.ReadInt32(nameof(Http2Limits.InitialConnectionWindowSize));
        if (i.HasValue) limits.InitialConnectionWindowSize = i.Value;

        i = section.ReadInt32(nameof(Http2Limits.InitialStreamWindowSize));
        if (i.HasValue) limits.InitialStreamWindowSize = i.Value;

        var t = section.ReadTimeSpan(nameof(Http2Limits.KeepAlivePingDelay));
        if (t.HasValue) limits.KeepAlivePingDelay = t.Value;

        t = section.ReadTimeSpan(nameof(Http2Limits.KeepAlivePingTimeout));
        if (t.HasValue) limits.KeepAlivePingTimeout = t.Value;

        i = section.ReadInt32(nameof(Http2Limits.MaxFrameSize));
        if (i.HasValue) limits.MaxFrameSize = i.Value;

        i = section.ReadInt32(nameof(Http2Limits.MaxRequestHeaderFieldSize));
        if (i.HasValue) limits.MaxRequestHeaderFieldSize = i.Value;

        i = section.ReadInt32(nameof(Http2Limits.MaxStreamsPerConnection));
        if (i.HasValue) limits.MaxStreamsPerConnection = i.Value;
    }

    private void ConfigureHttp3Limits(Http3Limits limits, IConfigurationSection section)
    {
        if (!section.Exists()) return;

        var i = section.ReadInt32(nameof(Http3Limits.MaxRequestHeaderFieldSize));
        if (i.HasValue) limits.MaxRequestHeaderFieldSize = i.Value;
    }

    private MinDataRate? CreateMinDataRate(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new MinDataRate(section.ReadDouble(nameof(MinDataRate.BytesPerSecond)).GetValueOrDefault(240),
            section.ReadTimeSpan(nameof(MinDataRate.GracePeriod)).GetValueOrDefault(TimeSpan.FromSeconds(5)));
    }
}