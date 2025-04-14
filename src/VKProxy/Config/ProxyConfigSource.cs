using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Collections.Frozen;
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
            var c = new ProxyConfigSnapshot();
            c.Routes = configuration.GetSection(nameof(ProxyConfigSnapshot.Routes)).GetChildren().Select(CreateRoute).ToFrozenDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
            c.Clusters = configuration.GetSection(nameof(ProxyConfigSnapshot.Clusters)).GetChildren().Select(CreateCluster).ToFrozenDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
            c.Listen = configuration.GetSection(nameof(ProxyConfigSnapshot.Listen)).GetChildren().Select(CreateListen).ToFrozenDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
            snapshot = c;

            var oldToken = cts;
            cts = new CancellationTokenSource();
            oldToken?.Cancel(throwOnFirstException: false);
        }
        subscription = ChangeToken.OnChange(section.GetReloadToken, UpdateSnapshot);
    }

    private RouteConfig CreateRoute(IConfigurationSection section)
    {
        return new RouteConfig()
        {
            Id = section.Key,
        };
    }

    private ListenConfig CreateListen(IConfigurationSection section)
    {
        return new ListenConfig()
        {
            Key = section.Key,
        };
    }

    private ClusterConfig CreateCluster(IConfigurationSection section)
    {
        return new ClusterConfig()
        {
            Id = section.Key,
        };
    }

    public void Dispose()
    {
        subscription?.Dispose();
        subscription = null;
    }
}

public class ProxyConfigSnapshot : IProxyConfig
{
    public IReadOnlyDictionary<string, RouteConfig> Routes { get; set; }

    public IReadOnlyDictionary<string, ClusterConfig> Clusters { get; set; }

    public IReadOnlyDictionary<string, ListenConfig> Listen { get; set; }
}