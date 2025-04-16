using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
    private readonly ReverseProxyOptions options;
    private readonly Lock configChangedLock = new Lock();

    public IProxyConfig CurrentSnapshot => snapshot;

    public IChangeToken? GetChangeToken()
    {
        return cts == null ? null : new CancellationChangeToken(cts.Token);
    }

    public ProxyConfigSource(IConfiguration configuration, IOptions<ReverseProxyOptions> options)
    {
        this.configuration = configuration;
        this.options = options.Value;
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
        var s = new SniConfig()
        {
            Key = section.Key,
            Host = section.GetSection(nameof(SniConfig.Host)).ReadStringArray(),
            Certificate = CreateSslConfig(section.GetSection(nameof(SniConfig.Certificate))),
            Order = section.ReadInt32(nameof(SniConfig.Order)).GetValueOrDefault(),
            RouteId = section[nameof(SniConfig.RouteId)],
            Passthrough = section.ReadBool(nameof(SniConfig.Passthrough)).GetValueOrDefault(),
            CheckCertificateRevocation = section.ReadBool(nameof(SniConfig.CheckCertificateRevocation)),
            Protocols = section.ReadEnum<SslProtocols>(nameof(SniConfig.Protocols)),
            ClientCertificateMode = section.ReadEnum<ClientCertificateMode>(nameof(SniConfig.ClientCertificateMode)),
        };

        var t = section.ReadTimeSpan(nameof(SniConfig.HandshakeTimeout));
        if (t.HasValue) s.HandshakeTimeout = t.Value;
        return s;
    }

    private CertificateConfig CreateSslConfig(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        var s = new CertificateConfig()
        {
            Path = section[nameof(CertificateConfig.Path)],
            KeyPath = section[nameof(CertificateConfig.KeyPath)],
            Password = section[nameof(CertificateConfig.Password)],
            Subject = section[nameof(CertificateConfig.Subject)],
            Store = section[nameof(CertificateConfig.Store)],
            Location = section[nameof(CertificateConfig.Location)],
            AllowInvalid = section.ReadBool(nameof(CertificateConfig.AllowInvalid))
        };

        return s;
    }

    private RouteConfig CreateRoute(IConfigurationSection section)
    {
        return new RouteConfig()
        {
            Key = section.Key,
            Order = section.ReadInt32(nameof(RouteConfig.Order)).GetValueOrDefault(),
            ClusterId = section[nameof(RouteConfig.ClusterId)],
            RetryCount = section.ReadInt32(nameof(RouteConfig.RetryCount)).GetValueOrDefault(),
            UdpResponses = section.ReadInt32(nameof(RouteConfig.UdpResponses)).GetValueOrDefault(),
            Timeout = section.ReadTimeSpan(nameof(RouteConfig.Timeout)).GetValueOrDefault(options.DefaultProxyTimeout),
            Protocols = section.ReadGatewayProtocols(nameof(RouteConfig.Protocols)).GetValueOrDefault(GatewayProtocols.HTTP1),
            Match = CreateRouteMatch(section.GetSection(nameof(RouteConfig.Match))),
        };
    }

    private RouteMatch CreateRouteMatch(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new RouteMatch()
        {
            Hosts = section.GetSection(nameof(RouteMatch.Hosts)).ReadStringArray()
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
            SniId = section[nameof(ListenConfig.SniId)],
            RouteId = section[nameof(ListenConfig.RouteId)]
        };
    }

    private ClusterConfig CreateCluster(IConfigurationSection section)
    {
        return new ClusterConfig()
        {
            Key = section.Key,
            LoadBalancingPolicy = section[nameof(ClusterConfig.LoadBalancingPolicy)],
            HealthCheck = CreateHealthCheck(section.GetSection(nameof(ClusterConfig.HealthCheck))),
            Destinations = section.GetSection(nameof(ClusterConfig.Destinations)).GetChildren().Select(CreateDestination).ToList()
        };
    }

    private DestinationConfig CreateDestination(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new DestinationConfig()
        {
            Address = section[nameof(DestinationConfig.Address)],
            Host = section[nameof(DestinationConfig.Host)],
        };
    }

    private HealthCheckConfig CreateHealthCheck(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new HealthCheckConfig()
        {
            Passive = CreatePassiveHealthCheckConfig(section.GetSection(nameof(HealthCheckConfig.Passive))),
            Active = CreateActiveHealthCheckConfig(section.GetSection(nameof(HealthCheckConfig.Active)))
        };
    }

    private PassiveHealthCheckConfig CreatePassiveHealthCheckConfig(IConfigurationSection section)
    {
        if (!section.Exists() || !section.ReadBool("Enable").GetValueOrDefault()) return null;
        var s = new PassiveHealthCheckConfig();
        s.DetectionWindowSize = section.ReadTimeSpan(nameof(PassiveHealthCheckConfig.DetectionWindowSize)).GetValueOrDefault(s.DetectionWindowSize);
        s.MinimalTotalCountThreshold = section.ReadInt32(nameof(PassiveHealthCheckConfig.MinimalTotalCountThreshold)).GetValueOrDefault(s.MinimalTotalCountThreshold);
        s.FailureRateLimit = section.ReadDouble(nameof(PassiveHealthCheckConfig.FailureRateLimit)).GetValueOrDefault(s.FailureRateLimit);
        s.ReactivationPeriod = section.ReadTimeSpan(nameof(PassiveHealthCheckConfig.ReactivationPeriod)).GetValueOrDefault(s.ReactivationPeriod);
        return s;
    }

    private ActiveHealthCheckConfig CreateActiveHealthCheckConfig(IConfigurationSection section)
    {
        if (!section.Exists() || !section.ReadBool("Enable").GetValueOrDefault()) return null;
        var s = new ActiveHealthCheckConfig();
        s.Interval = section.ReadTimeSpan(nameof(ActiveHealthCheckConfig.Interval)).GetValueOrDefault(s.Interval);
        s.Timeout = section.ReadTimeSpan(nameof(ActiveHealthCheckConfig.Timeout)).GetValueOrDefault(s.Timeout);
        s.Policy = section[nameof(ActiveHealthCheckConfig.Policy)] ?? s.Policy;
        s.Path = section[nameof(ActiveHealthCheckConfig.Path)];
        s.Query = section[nameof(ActiveHealthCheckConfig.Query)];
        s.Passes = section.ReadInt32(nameof(ActiveHealthCheckConfig.Passes)).GetValueOrDefault(s.Passes);
        s.Fails = section.ReadInt32(nameof(ActiveHealthCheckConfig.Fails)).GetValueOrDefault(s.Fails);
        return s;
    }

    public void Dispose()
    {
        subscription?.Dispose();
        subscription = null;
    }
}