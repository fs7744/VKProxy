using dotnet_etcd.interfaces;
using Etcdserverpb;
using Google.Protobuf;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using VKProxy.Config;

namespace VKProxy.Storages.Etcd;

public class EtcdProxyConfigSource : IConfigSource<IProxyConfig>
{
    private readonly IEtcdClient client;
    private readonly string prefix;
    private CancellationTokenSource cts;
    private ProxyConfigSnapshot currentSnapshot;
    private readonly Lock configChangedLock = new Lock();
    private readonly List<ListenEndPointOptions> stop = new();
    private readonly List<ListenEndPointOptions> start = new();

    public IProxyConfig CurrentSnapshot => currentSnapshot;

    public EtcdProxyConfigSource(IEtcdClient client, EtcdProxyConfigSourceOptions options)
    {
        this.client = client;
        this.prefix = options.Prefix;
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
            currentSnapshot = await LoadAllAsync(cancellationToken);
        }
        return (stop, start);
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
            }
            else if (key.StartsWith("sni/", StringComparison.OrdinalIgnoreCase))
            {
                var v = JsonSerializer.Deserialize<SniConfig>(kv.Value.Span);
                v.Key = key.Substring(4);
                sni.Add(v.Key, v);
            }
        }

        await client.WatchRangeAsync(prefix, Change);

        return new ProxyConfigSnapshot(routes, clusters, listen, sni);
    }

    private void Change(WatchResponse response)
    {
        lock (configChangedLock)
        {
            var hasListenChange = false;
            foreach (var evt in response.Events)
            {
                string key = GetUtf8String(evt.Kv.Key, prefix.Length);
                if (key.StartsWith("route/", StringComparison.OrdinalIgnoreCase))
                {
                    switch (evt.Type)
                    {
                        case Mvccpb.Event.Types.EventType.Put:
                            break;

                        case Mvccpb.Event.Types.EventType.Delete:

                            break;
                    }
                }
                else if (key.StartsWith("cluster/", StringComparison.OrdinalIgnoreCase))
                {
                }
                else if (key.StartsWith("listen/", StringComparison.OrdinalIgnoreCase))
                {
                    var k = key.Substring(7);
                    switch (evt.Type)
                    {
                        case Mvccpb.Event.Types.EventType.Put:
                            var vv = JsonSerializer.Deserialize<ListenConfig>(evt.Kv.Value.Span);
                            if (currentSnapshot.Listen.TryGetValue(k, out var v) && !ListenConfig.Equals(v, vv))
                            {
                                stop.AddRange(v.Options);
                                start.AddRange(vv.Options);
                                hasListenChange = true;
                            }
                            break;

                        case Mvccpb.Event.Types.EventType.Delete:
                            if (currentSnapshot.Listen.TryGetValue(k, out v) && v.Options != null)
                            {
                                stop.AddRange(v.Options);
                                hasListenChange = true;
                            }
                            currentSnapshot.RemoveListen(k);
                            break;
                    }
                }
                else if (key.StartsWith("sni/", StringComparison.OrdinalIgnoreCase))
                {
                }
            }

            if (hasListenChange)
            {
                var old = cts;
                cts = new CancellationTokenSource();
                old.Cancel(true);
                stop.Clear();
                start.Clear();
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