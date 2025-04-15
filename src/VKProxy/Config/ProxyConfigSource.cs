using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Security.Authentication;
using VKProxy.Core.Config;

namespace VKProxy.Config;

internal class ProxyConfigSource : IConfigSource<IProxyConfig>
{
    private CancellationTokenSource cts;
    private ProxyConfigSnapshot snapshot;
    private IDisposable subscription;
    private readonly IConfiguration configuration;
    private readonly Lock configChangedLock = new Lock();

    public IProxyConfig CurrentSnapshot => snapshot;

    public IChangeToken? GetChangeToken()
    {
        return cts == null ? null : new CancellationChangeToken(cts.Token);
    }

    public ProxyConfigSource(IConfiguration configuration)
    {
        this.configuration = configuration;
        UpdateSnapshot();
    }

    private void UpdateSnapshot()
    {
        var section = configuration.GetSection("ReverseProxy");
        if (!section.Exists()) return;
        lock (configChangedLock)
        {
            var c = new ProxyConfigSnapshot(
                section.GetSection(nameof(ProxyConfigSnapshot.Routes)).GetChildren().Select(CreateRoute).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase)
                , section.GetSection(nameof(ProxyConfigSnapshot.Clusters)).GetChildren().Select(CreateCluster).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase)
                , section.GetSection(nameof(ProxyConfigSnapshot.Listen)).GetChildren().Select(CreateListen).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase)
                , section.GetSection(nameof(ProxyConfigSnapshot.Sni)).GetChildren().Select(CreateSni).ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase));
            snapshot = c;

            var oldToken = cts;
            cts = new CancellationTokenSource();
            oldToken?.Cancel(throwOnFirstException: false);
        }
        subscription = ChangeToken.OnChange(section.GetReloadToken, UpdateSnapshot);
    }

    private SniConfig CreateSni(IConfigurationSection section, int arg2)
    {
        return new SniConfig()
        {
            Key = section.Key,
            Host = section.GetSection(nameof(SniConfig.Host)).ReadStringArray(),
            Tls = CreateSslConfig(section.GetSection(nameof(SniConfig.Tls))),
            Order = section.ReadInt32(nameof(SniConfig.Order)).GetValueOrDefault()
        };
    }

    private SslConfig CreateSslConfig(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        var s = new SslConfig()
        {
            Path = section[nameof(SslConfig.Path)],
            KeyPath = section[nameof(SslConfig.KeyPath)],
            Password = section[nameof(SslConfig.Password)],
            Subject = section[nameof(SslConfig.Subject)],
            Store = section[nameof(SslConfig.Store)],
            Location = section[nameof(SslConfig.Location)],
            AllowInvalid = section.ReadBool(nameof(SslConfig.AllowInvalid))
        };

        //s.SupportSslProtocols = section.ReadSslProtocols(nameof(SslConfig.SupportSslProtocols)).GetValueOrDefault(s.SupportSslProtocols);
        //s.Passthrough = section.ReadBool(nameof(SslConfig.Passthrough)).GetValueOrDefault(s.Passthrough);
        //s.HandshakeTimeout = section.ReadTimeSpan(nameof(SslConfig.HandshakeTimeout)).GetValueOrDefault(s.HandshakeTimeout);
        //s.ClientCertificateRequired = section.ReadBool(nameof(SslConfig.ClientCertificateRequired)).GetValueOrDefault(s.ClientCertificateRequired);
        //s.CheckCertificateRevocation = section.ReadBool(nameof(SslConfig.CheckCertificateRevocation)).GetValueOrDefault(s.CheckCertificateRevocation);
        return s;
    }

    private RouteConfig CreateRoute(IConfigurationSection section)
    {
        return new RouteConfig()
        {
            Key = section.Key,
        };
    }

    private ListenConfig CreateListen(IConfigurationSection section)
    {
        return new ListenConfig()
        {
            Key = section.Key,
            Protocols = section.ReadGatewayProtocols(nameof(ListenConfig.Protocols)).GetValueOrDefault(GatewayProtocols.HTTP1),
            Address = section.GetSection(nameof(ListenConfig.Address)).ReadStringArray(),
            UseSni = section.ReadBool(nameof(ListenConfig.UseSni)).GetValueOrDefault(),
            CheckCertificateRevocation = section.ReadBool(nameof(ListenConfig.CheckCertificateRevocation)),
            HandshakeTimeout = section.ReadTimeSpan(nameof(ListenConfig.HandshakeTimeout)),
            TlsProtocols = section.ReadEnum<SslProtocols>(nameof(ListenConfig.TlsProtocols)),
            ClientCertificateMode = section.ReadEnum<ClientCertificateMode>(nameof(ListenConfig.ClientCertificateMode)),
            SniId = section[nameof(ListenConfig.SniId)],
            RouteId = section[nameof(ListenConfig.RouteId)]
        };
    }

    private ClusterConfig CreateCluster(IConfigurationSection section)
    {
        return new ClusterConfig()
        {
            Key = section.Key,
        };
    }

    public void Dispose()
    {
        subscription?.Dispose();
        subscription = null;
    }
}