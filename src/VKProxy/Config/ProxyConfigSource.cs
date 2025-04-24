using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.Frozen;
using System.Security.Authentication;
using VKProxy.Config.Validators;
using VKProxy.Core.Config;
using VKProxy.Core.Loggers;
using VKProxy.Health;

namespace VKProxy.Config;

internal class ProxyConfigSource : IConfigSource<IProxyConfig>
{
    private CancellationTokenSource cts;
    private ProxyConfigSnapshot snapshot;
    private ProxyConfigSnapshot old;
    private IDisposable subscription;
    private readonly IConfiguration configuration;
    private readonly ProxyLogger logger;
    private readonly ReverseProxyOptions options;
    private readonly Lock configChangedLock = new Lock();
    private readonly IValidator<IProxyConfig> validator;
    private readonly IHttpSelector httpSelector;
    private readonly ISniSelector sniSelector;
    private readonly IActiveHealthCheckMonitor healthCheckMonitor;

    public IProxyConfig CurrentSnapshot => snapshot;

    public IChangeToken? GetChangeToken()
    {
        return cts == null ? null : new CancellationChangeToken(cts.Token);
    }

    public ProxyConfigSource(IConfiguration configuration, IOptions<ReverseProxyOptions> options, ProxyLogger logger,
        IValidator<IProxyConfig> validator, IHttpSelector httpSelector, ISniSelector sniSelector, IActiveHealthCheckMonitor healthCheckMonitor)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.options = options.Value;
        this.validator = validator;
        this.httpSelector = httpSelector;
        this.sniSelector = sniSelector;
        this.healthCheckMonitor = healthCheckMonitor;
        UpdateSnapshot();
        var section = configuration.GetSection("ReverseProxy");
        subscription = ChangeToken.OnChange(section.GetReloadToken, UpdateSnapshot);
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
            if (old == null)
                old = snapshot;
            snapshot = c;
            var oldToken = cts;
            cts = new CancellationTokenSource();
            oldToken?.Cancel(throwOnFirstException: false);
        }
    }

    private static SniConfig CreateSni(IConfigurationSection section, int arg2)
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

    private static CertificateConfig CreateSslConfig(IConfigurationSection section)
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
            UdpResponses = section.ReadInt32(nameof(RouteConfig.UdpResponses)).GetValueOrDefault(),
            Timeout = section.ReadTimeSpan(nameof(RouteConfig.Timeout)).GetValueOrDefault(options.DefaultProxyTimeout),
            Match = CreateRouteMatch(section.GetSection(nameof(RouteConfig.Match))),
            Metadata = section.GetSection(nameof(RouteConfig.Metadata)).ReadStringDictionary(),
            Transforms = CreateTransforms(section.GetSection(nameof(RouteConfig.Transforms)))
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> CreateTransforms(IConfigurationSection section)
    {
        if (section.GetChildren() is var children && !children.Any())
        {
            return null;
        }

        return children
            .Select(subSection => subSection.GetChildren().ToDictionary(d => d.Key, d => d.Value!, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static RouteMatch CreateRouteMatch(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new RouteMatch()
        {
            Hosts = section.GetSection(nameof(RouteMatch.Hosts)).ReadStringArray(),
            Paths = section.GetSection(nameof(RouteMatch.Paths)).ReadStringArray(),
            Methods = section.GetSection(nameof(RouteMatch.Methods)).ReadStringArray()?.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            Statement = section[nameof(RouteMatch.Statement)]
        };
    }

    private static ListenConfig CreateListen(IConfigurationSection section)
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

    private static ClusterConfig CreateCluster(IConfigurationSection section)
    {
        return new ClusterConfig()
        {
            Key = section.Key,
            LoadBalancingPolicy = section[nameof(ClusterConfig.LoadBalancingPolicy)],
            HealthCheck = CreateHealthCheck(section.GetSection(nameof(ClusterConfig.HealthCheck))),
            Destinations = section.GetSection(nameof(ClusterConfig.Destinations)).GetChildren().Select(CreateDestination).ToList(),
            HttpClientConfig = CreateHttpClientConfig(section.GetSection(nameof(ClusterConfig.HttpClientConfig))),
            HttpRequest = CreateProxyRequestConfig(section.GetSection(nameof(ClusterConfig.HttpRequest))),
        };
    }

    private static ForwarderRequestConfig? CreateProxyRequestConfig(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return null;
        }

        return new ForwarderRequestConfig
        {
            ActivityTimeout = section.ReadTimeSpan(nameof(ForwarderRequestConfig.ActivityTimeout)),
            Version = section.ReadVersion(nameof(ForwarderRequestConfig.Version)),
            VersionPolicy = section.ReadEnum<HttpVersionPolicy>(nameof(ForwarderRequestConfig.VersionPolicy)),
            AllowResponseBuffering = section.ReadBool(nameof(ForwarderRequestConfig.AllowResponseBuffering))
        };
    }

    private static HttpClientConfig CreateHttpClientConfig(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new HttpClientConfig
        {
            SslProtocols = section.ReadSslProtocols(nameof(HttpClientConfig.SslProtocols)),
            DangerousAcceptAnyServerCertificate = section.ReadBool(nameof(HttpClientConfig.DangerousAcceptAnyServerCertificate)),
            MaxConnectionsPerServer = section.ReadInt32(nameof(HttpClientConfig.MaxConnectionsPerServer)),
            EnableMultipleHttp2Connections = section.ReadBool(nameof(HttpClientConfig.EnableMultipleHttp2Connections)),
            RequestHeaderEncoding = section[nameof(HttpClientConfig.RequestHeaderEncoding)],
            ResponseHeaderEncoding = section[nameof(HttpClientConfig.ResponseHeaderEncoding)],
            WebProxy = CreateWebProxy(section.GetSection(nameof(HttpClientConfig.WebProxy)))
        };
    }

    private static WebProxyConfig CreateWebProxy(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new WebProxyConfig()
        {
            Address = section.ReadUri(nameof(WebProxyConfig.Address)),
            BypassOnLocal = section.ReadBool(nameof(WebProxyConfig.BypassOnLocal)),
            UseDefaultCredentials = section.ReadBool(nameof(WebProxyConfig.UseDefaultCredentials))
        };
    }

    private static DestinationConfig CreateDestination(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new DestinationConfig()
        {
            Address = section[nameof(DestinationConfig.Address)],
            Host = section[nameof(DestinationConfig.Host)],
        };
    }

    private static HealthCheckConfig CreateHealthCheck(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        return new HealthCheckConfig()
        {
            Passive = CreatePassiveHealthCheckConfig(section.GetSection(nameof(HealthCheckConfig.Passive))),
            Active = CreateActiveHealthCheckConfig(section.GetSection(nameof(HealthCheckConfig.Active)))
        };
    }

    private static PassiveHealthCheckConfig CreatePassiveHealthCheckConfig(IConfigurationSection section)
    {
        if (!section.Exists() || !section.ReadBool("Enable").GetValueOrDefault()) return null;
        var s = new PassiveHealthCheckConfig();
        s.DetectionWindowSize = section.ReadTimeSpan(nameof(PassiveHealthCheckConfig.DetectionWindowSize)).GetValueOrDefault(s.DetectionWindowSize);
        s.MinimalTotalCountThreshold = section.ReadInt32(nameof(PassiveHealthCheckConfig.MinimalTotalCountThreshold)).GetValueOrDefault(s.MinimalTotalCountThreshold);
        s.FailureRateLimit = section.ReadDouble(nameof(PassiveHealthCheckConfig.FailureRateLimit)).GetValueOrDefault(s.FailureRateLimit);
        s.ReactivationPeriod = section.ReadTimeSpan(nameof(PassiveHealthCheckConfig.ReactivationPeriod)).GetValueOrDefault(s.ReactivationPeriod);
        return s;
    }

    private static ActiveHealthCheckConfig CreateActiveHealthCheckConfig(IConfigurationSection section)
    {
        if (!section.Exists() || !section.ReadBool("Enable").GetValueOrDefault()) return null;
        var s = new ActiveHealthCheckConfig();
        s.Interval = section.ReadTimeSpan(nameof(ActiveHealthCheckConfig.Interval)).GetValueOrDefault(s.Interval);
        s.Timeout = section.ReadTimeSpan(nameof(ActiveHealthCheckConfig.Timeout)).GetValueOrDefault(s.Timeout);
        s.Policy = section[nameof(ActiveHealthCheckConfig.Policy)] ?? s.Policy;
        s.Path = section[nameof(ActiveHealthCheckConfig.Path)];
        s.Query = section[nameof(ActiveHealthCheckConfig.Query)];
        s.Method = section[nameof(ActiveHealthCheckConfig.Method)];
        s.Passes = section.ReadInt32(nameof(ActiveHealthCheckConfig.Passes)).GetValueOrDefault(s.Passes);
        s.Fails = section.ReadInt32(nameof(ActiveHealthCheckConfig.Fails)).GetValueOrDefault(s.Fails);
        return s;
    }

    public void Dispose()
    {
        subscription?.Dispose();
        subscription = null;
    }

    public async Task<(IEnumerable<ListenEndPointOptions> stop, IEnumerable<ListenEndPointOptions> start)> GenerateDiffAsync(CancellationToken cancellationToken)
    {
        var current = snapshot;
        if (current == null && old == null) return (null, null);

        if (old != null)
        {
            foreach (var (k, v) in old.Clusters)
            {
                if (current.Clusters.TryGetValue(k, out var cluster)
                    && v.Equals(cluster))
                {
                    cluster.HealthReporter = v.HealthReporter;
                    cluster.DestinationStates = v.DestinationStates;
                    cluster.LoadBalancingPolicyInstance = v.LoadBalancingPolicyInstance;
                    cluster.AvailableDestinations = v.AvailableDestinations;
                    cluster.HealthCheck = v.HealthCheck;
                    cluster.Destinations = v.Destinations;
                    cluster.HttpMessageHandler = v.HttpMessageHandler;
                }
            }

            foreach (var (k, v) in old.Routes)
            {
                if (current.Routes.TryGetValue(k, out var r)
                    && v.Equals(r))
                {
                    current.ReplaceRoute(k, v);
                }
            }

            foreach (var (k, v) in old.Listen)
            {
                if (current.Listen.TryGetValue(k, out var r)
                    && v.Equals(r))
                {
                    current.ReplaceListen(k, v);
                }
            }
        }

        var errors = new List<Exception>();
        if (!await validator.ValidateAsync(current, errors, cancellationToken))
        {
            foreach (var error in errors)
            {
                logger.ErrorConfig(error.Message);
            }
        }

        healthCheckMonitor.CheckHealthAsync(current.Clusters.Values);

        bool sniChanged = false;
        bool httpChanged = false;
        if (old == null)
        {
            sniChanged = true;
            httpChanged = true;
        }
        else
        {
            if (old.Sni.Count != current.Sni.Count
                || old.Sni.Keys.Intersect(current.Sni.Keys, StringComparer.OrdinalIgnoreCase).Count() != current.Sni.Count)
            {
                sniChanged = true;
            }
            else
            {
                foreach (var (k, v) in old.Sni)
                {
                    if (!current.Sni.TryGetValue(k, out var sni)
                        || !v.Equals(sni))
                    {
                        sniChanged = true;
                        break;
                    }
                }
            }

            var ov = old.Routes.Values.Where(i => i.Match != null && i.Match.Hosts != null && i.Match.Hosts.Count != 0 && i.Match.Paths != null && i.Match.Paths.Count != 0).ToArray();
            var cv = current.Routes.Values.Where(i => i.Match != null && i.Match.Hosts != null && i.Match.Hosts.Count != 0 && i.Match.Paths != null && i.Match.Paths.Count != 0).ToArray();

            if (ov.Length != cv.Length
                || ov.Select(i => i.Key).Intersect(cv.Select(i => i.Key), StringComparer.OrdinalIgnoreCase).Count() != cv.Length)
            {
                httpChanged = true;
            }
            else
            {
                foreach (var v in ov)
                {
                    if (!current.Routes.TryGetValue(v.Key, out var r)
                        || !v.Equals(r))
                    {
                        httpChanged = true;
                        break;
                    }
                }
            }
        }

        if (sniChanged)
            await sniSelector.ReBuildAsync(current.Sni, cancellationToken);
        if (httpChanged)
            await httpSelector.ReBuildAsync(current.Routes, cancellationToken);

        if (old == null)
            return (null, current?.Listen.Values.SelectMany(i => i.ListenEndPointOptions));

        var stop = new List<ListenEndPointOptions>();
        var start = new List<ListenEndPointOptions>();

        foreach (var (k, v) in old.Listen)
        {
            if (current.Listen.TryGetValue(k, out var cv))
            {
                if (!cv.Equals(v))
                {
                    stop.AddRange(v.ListenEndPointOptions);
                    start.AddRange(cv.ListenEndPointOptions);
                }
            }
            else
            {
                stop.AddRange(v.ListenEndPointOptions);
            }
        }

        foreach (var (k, cv) in current.Listen)
        {
            if (!old.Listen.TryGetValue(k, out var v))
            {
                start.AddRange(cv.ListenEndPointOptions);
            }
        }
        old = null;
        return (stop, start);
    }
}