using Etcd;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Mvccpb;
using System.Text;
using System.Text.Json;
using VKProxy.Config;
using VKProxy.Config.Validators;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;
using VKProxy.Health;

namespace VKProxy.Storages.Etcd;

public class EtcdProxyConfigSource : IConfigSource<IProxyConfig>
{
    private readonly IEtcdClient client;
    private readonly ProxyLogger logger;
    private readonly IValidator<IProxyConfig> validator;
    private readonly IHttpSelector httpSelector;
    private readonly ISniSelector sniSelector;
    private readonly IActiveHealthCheckMonitor healthCheckMonitor;
    private readonly string prefix;
    private readonly TimeSpan delay;
    private CancellationTokenSource cts;
    private ProxyConfigSnapshot currentSnapshot;
    private readonly Lock configChangedLock = new Lock();
    private readonly List<ListenConfig> stop = new();
    private readonly List<ListenConfig> start = new();

    public IProxyConfig CurrentSnapshot => currentSnapshot;

    public EtcdProxyConfigSource([FromKeyedServices(nameof(EtcdProxyConfigSource))] IEtcdClient client, EtcdProxyConfigSourceOptions options, ProxyLogger logger,
        IValidator<IProxyConfig> validator, IHttpSelector httpSelector, ISniSelector sniSelector, IActiveHealthCheckMonitor healthCheckMonitor)
    {
        this.client = client;
        this.logger = logger;
        this.validator = validator;
        this.httpSelector = httpSelector;
        this.sniSelector = sniSelector;
        this.healthCheckMonitor = healthCheckMonitor;
        this.prefix = options.Prefix;
        this.delay = options.Delay.GetValueOrDefault(EtcdHostBuilderExtensions.defaultDelay);
        cts = new CancellationTokenSource();
    }

    public void Dispose()
    {
        client?.Dispose();
    }

    public async Task<(IEnumerable<ListenEndPointOptions> stop, IEnumerable<ListenEndPointOptions> start)> GenerateDiffAsync(CancellationToken cancellationToken)
    {
        if (currentSnapshot is null)
        {
            await LoadAllAsync(cancellationToken);
        }
        var r = (stop.SelectMany(i =>
        {
            var o = i.Options;
            return o == null ? Array.Empty<ListenEndPointOptions>() : (IEnumerable<ListenEndPointOptions>)o;
        }).ToArray(), start.SelectMany(i =>
        {
            var o = i.Options;
            return o == null ? Array.Empty<ListenEndPointOptions>() : (IEnumerable<ListenEndPointOptions>)o;
        }).ToArray());

        stop.Clear();
        start.Clear();
        return r;
    }

    private async Task<ProxyConfigSnapshot?> LoadAllAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, RouteConfig> routes = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ClusterConfig> clusters = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ListenConfig> listen = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, SniConfig> sni = new(StringComparer.OrdinalIgnoreCase);

        var response = await client.GetRangeAsync(prefix, cancellationToken: cancellationToken);
        foreach (var kv in response.Kvs)
        {
            string key = GetUtf8String(kv.Key, prefix.Length);
            if (key.StartsWith("route/", StringComparison.OrdinalIgnoreCase))
            {
                var v = JsonSerializer.Deserialize<RouteConfig>(kv.Value.Span);
                v.Key = key.Substring(6);
                routes.Add(v.Key, v);
            }
            else if (key.StartsWith("cluster/", StringComparison.OrdinalIgnoreCase))
            {
                var v = JsonSerializer.Deserialize<ClusterConfig>(kv.Value.Span);
                v.Key = key.Substring(8);
                clusters.Add(v.Key, v);
            }
            else if (key.StartsWith("listen/", StringComparison.OrdinalIgnoreCase))
            {
                var v = JsonSerializer.Deserialize<ListenConfig>(kv.Value.Span);
                v.Key = key.Substring(7);
                listen.Add(v.Key, v);
                start.Add(v);
            }
            else if (key.StartsWith("sni/", StringComparison.OrdinalIgnoreCase))
            {
                var v = JsonSerializer.Deserialize<SniConfig>(kv.Value.Span);
                v.Key = key.Substring(4);
                sni.Add(v.Key, v);
            }
        }

        var config = new ProxyConfigSnapshot(routes, clusters, listen, sni);
        var errors = new List<Exception>();
        if (!await validator.ValidateAsync(config, errors, cancellationToken))
        {
            foreach (var error in errors)
            {
                logger.ErrorConfig(error.Message);
            }
        }

        healthCheckMonitor.CheckHealthAsync(config.Clusters.Values);
        if (config.Sni.Count > 0)
            await sniSelector.ReBuildAsync(config.Sni, cancellationToken);
        if (config.Routes.Values.Any(i => i.Match != null && i.Match.Hosts != null && i.Match.Hosts.Count != 0 && i.Match.Paths != null && i.Match.Paths.Count != 0))
        {
            await httpSelector.ReBuildAsync(config.Routes, cancellationToken);
        }
        currentSnapshot = config;
        await Task.Factory.StartNew(async () =>
        {
            long startRevision = 0;
            while (true)
            {
                try
                {
                    try
                    {
                        using var watcher1 = await client.WatchRangeAsync(prefix, startRevision: startRevision);
                        await watcher1.ForAllAsync(i =>
                        {
                            if (i.Events.Any())
                            {
                                throw new InvalidOperationException();
                            }
                            return Task.CompletedTask;
                        });
                    }
                    catch
                    {
                    }
                    await Task.Delay(delay);

                    var cts = CancellationTokenSourcePool.Default.Rent();
                    cts.CancelAfter(delay);
                    using var watcher = await client.WatchRangeAsync(prefix, startRevision: startRevision, cancellationToken: cts.Token);
                    await watcher.ForAllAsync(i =>
                    {
                        startRevision = i.FindRevision(startRevision);
                        if (!i.Events.Any()) return Task.CompletedTask;
                        return ChangeAsync(i.Events);
                    }, cancellationToken: cts.Token);
                }
                catch (OperationCanceledException)
                { }
                catch (Exception ex)
                {
                    logger.UnexpectedException(ex.Message, ex);
                }
                await Task.Delay(delay);
            }
        }, TaskCreationOptions.LongRunning);
        return config;
    }

    private async Task ChangeAsync(RepeatedField<Event> events)
    {
        lock (configChangedLock)
        {
            var hasListenChange = false;
            var hasHttpChange = false;
            var hasSniChange = false;
            foreach (var evt in events)
            {
                string key = GetUtf8String(evt.Kv.Key, prefix.Length);
                if (key.StartsWith("route/", StringComparison.OrdinalIgnoreCase))
                {
                    var k = key.Substring(6);
                    switch (evt.Type)
                    {
                        case Mvccpb.Event.Types.EventType.Put:
                            var vv = JsonSerializer.Deserialize<RouteConfig>(evt.Kv.Value.Span);
                            vv.Key = k;
                            if (currentSnapshot.Routes.TryGetValue(k, out var v))
                            {
                                if (!RouteConfig.Equals(v, vv))
                                {
                                    currentSnapshot.ReplaceRoute(k, vv);
                                    if (v.Match != null && v.Match.Hosts != null && v.Match.Hosts.Count != 0 && v.Match.Paths != null && v.Match.Paths.Count != 0)
                                    {
                                        hasHttpChange = true;
                                    }
                                }
                            }
                            else
                            {
                                currentSnapshot.ReplaceRoute(k, vv);
                                if (v.Match != null && v.Match.Hosts != null && v.Match.Hosts.Count != 0 && v.Match.Paths != null && v.Match.Paths.Count != 0)
                                {
                                    hasHttpChange = true;
                                }
                            }
                            break;

                        case Mvccpb.Event.Types.EventType.Delete:
                            if (currentSnapshot.Routes.TryGetValue(k, out var i) && i.Match != null && i.Match.Hosts != null && i.Match.Hosts.Count != 0 && i.Match.Paths != null && i.Match.Paths.Count != 0)
                            {
                                hasHttpChange = true;
                            }
                            currentSnapshot.RemoveRoute(k);
                            break;
                    }
                }
                else if (key.StartsWith("cluster/", StringComparison.OrdinalIgnoreCase))
                {
                    var k = key.Substring(8);
                    switch (evt.Type)
                    {
                        case Mvccpb.Event.Types.EventType.Put:
                            var vv = JsonSerializer.Deserialize<ClusterConfig>(evt.Kv.Value.Span);
                            vv.Key = k;
                            if (currentSnapshot.Clusters.TryGetValue(k, out var v))
                            {
                                if (!ClusterConfig.Equals(v, vv))
                                {
                                    currentSnapshot.ReplaceCluster(k, vv);
                                }
                            }
                            else
                            {
                                currentSnapshot.ReplaceCluster(k, vv);
                            }
                            break;

                        case Mvccpb.Event.Types.EventType.Delete:
                            currentSnapshot.RemoveCluster(k);
                            break;
                    }
                }
                else if (key.StartsWith("listen/", StringComparison.OrdinalIgnoreCase))
                {
                    var k = key.Substring(7);
                    switch (evt.Type)
                    {
                        case Mvccpb.Event.Types.EventType.Put:
                            var vv = JsonSerializer.Deserialize<ListenConfig>(evt.Kv.Value.Span);
                            vv.Key = k;
                            if (currentSnapshot.Listen.TryGetValue(k, out var v))
                            {
                                if (!ListenConfig.Equals(v, vv))
                                {
                                    stop.Add(v);
                                    start.Add(vv);
                                    currentSnapshot.ReplaceListen(k, vv);
                                    hasListenChange = true;
                                }
                            }
                            else
                            {
                                start.Add(vv);
                                currentSnapshot.ReplaceListen(k, vv);
                                hasListenChange = true;
                            }
                            break;

                        case Mvccpb.Event.Types.EventType.Delete:
                            if (currentSnapshot.Listen.TryGetValue(k, out v) && v.Options != null)
                            {
                                stop.Add(v);
                                hasListenChange = true;
                            }
                            currentSnapshot.RemoveListen(k);
                            break;
                    }
                }
                else if (key.StartsWith("sni/", StringComparison.OrdinalIgnoreCase))
                {
                    var k = key.Substring(4);
                    switch (evt.Type)
                    {
                        case Mvccpb.Event.Types.EventType.Put:
                            var vv = JsonSerializer.Deserialize<SniConfig>(evt.Kv.Value.Span);
                            vv.Key = k;
                            if (currentSnapshot.Sni.TryGetValue(k, out var v))
                            {
                                if (!SniConfig.Equals(v, vv))
                                {
                                    currentSnapshot.ReplaceSni(k, vv);
                                    hasListenChange = true;
                                }
                            }
                            else
                            {
                                currentSnapshot.ReplaceSni(k, vv);
                                hasListenChange = true;
                            }
                            break;

                        case Mvccpb.Event.Types.EventType.Delete:
                            if (currentSnapshot.Sni.TryGetValue(k, out v))
                            {
                                hasSniChange = true;
                            }
                            currentSnapshot.RemoveSni(k);
                            break;
                    }
                }
            }

            var errors = new List<Exception>();
            if (!validator.ValidateAsync(currentSnapshot, errors, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult())
            {
                foreach (var error in errors)
                {
                    logger.ErrorConfig(error.Message);
                }
            }
            healthCheckMonitor.CheckHealthAsync(currentSnapshot.Clusters.Values);
            if (hasSniChange)
                sniSelector.ReBuildAsync(currentSnapshot.Sni, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            if (hasHttpChange)
            {
                httpSelector.ReBuildAsync(currentSnapshot.Routes, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            if (hasListenChange)
            {
                var old = cts;
                cts = new CancellationTokenSource();
                old.Cancel(true);
            }
        }
    }

    public IChangeToken? GetChangeToken()
    {
        return new CancellationChangeToken(cts.Token);
    }

    public static string GetUtf8String(ByteString bytes)
    {
        return Encoding.UTF8.GetString(bytes.Span);
    }

    public static string GetUtf8String(ByteString bytes, int start)
    {
        return Encoding.UTF8.GetString(bytes.Span.Slice(start));
    }
}