using Etcd;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using VKProxy.Config;
using VKProxy.HttpRoutingStatement;

namespace VKProxy.Storages.Etcd;

internal class EtcdConfigStorage : IConfigStorage
{
    private readonly IEtcdClient client;
    private readonly EtcdProxyConfigSourceOptions options;

    public EtcdConfigStorage([FromKeyedServices(nameof(EtcdProxyConfigSource))] IEtcdClient client, EtcdProxyConfigSourceOptions options)
    {
        this.client = client;
        this.options = options;
    }

    public async Task<long> DeleteClusterAsync(string key)
    {
        var r = await client.DeleteAsync($"{options.Prefix}cluster/{key}");
        return r.Deleted;
    }

    public async Task<long> DeleteListenAsync(string key)
    {
        var r = await client.DeleteAsync($"{options.Prefix}listen/{key}");
        return r.Deleted;
    }

    public async Task<long> DeleteRouteAsync(string key)
    {
        var r = await client.DeleteAsync($"{options.Prefix}route/{key}");
        return r.Deleted;
    }

    public async Task<long> DeleteSniAsync(string key)
    {
        var r = await client.DeleteAsync($"{options.Prefix}sni/{key}");
        return r.Deleted;
    }

    public async Task<bool> ExistsClusterAsync(string key)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}cluster/{key}")
        });
        return r.Count > 0;
    }

    public async Task<bool> ExistsListenAsync(string key)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}listen/{key}")
        });
        return r.Count > 0;
    }

    public async Task<bool> ExistsRouteAsync(string key)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}route/{key}")
        });
        return r.Count > 0;
    }

    public async Task<bool> ExistsSniAsync(string key)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}sni/{key}")
        });
        return r.Count > 0;
    }

    public async Task<IEnumerable<ClusterConfig>> GetClusterAsync(string? prefix)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}cluster/{prefix}");
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<ClusterConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 8);
            return r;
        });
    }

    public async Task<IEnumerable<ListenConfig>> GetListenAsync(string prefix)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}listen/{prefix}");
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<ListenConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 7);
            return r;
        });
    }

    public async Task<IEnumerable<RouteConfig>> GetRouteAsync(string? prefix)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}route/{prefix}");
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<RouteConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 6);
            return r;
        });
    }

    public async Task<IEnumerable<SniConfig>> GetSniAsync(string? prefix)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}sni/{prefix}");
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<SniConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 4);
            return r;
        });
    }

    public async Task UpdateClusterAsync(ClusterConfig config)
    {
        await client.PutAsync($"{options.Prefix}cluster/{config.Key}", JsonSerializer.Serialize(config));
    }

    public async Task UpdateListenAsync(ListenConfig config)
    {
        await client.PutAsync($"{options.Prefix}listen/{config.Key}", JsonSerializer.Serialize(config));
    }

    public async Task UpdateRouteAsync(RouteConfig config)
    {
        if (config.Match != null && config.Match.Statement != null)
        {
            try
            {
                HttpRoutingStatementParser.ConvertToFunction(config.Match.Statement);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, "Statement");
            }
        }
        await client.PutAsync($"{options.Prefix}route/{config.Key}", JsonSerializer.Serialize(config));
    }

    public async Task UpdateSniAsync(SniConfig config)
    {
        await client.PutAsync($"{options.Prefix}sni/{config.Key}", JsonSerializer.Serialize(config));
    }
}