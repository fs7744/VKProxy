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
    private readonly IRouteStatementFactory statementFactory;

    public EtcdConfigStorage([FromKeyedServices(nameof(EtcdProxyConfigSource))] IEtcdClient client, EtcdProxyConfigSourceOptions options, IRouteStatementFactory statementFactory)
    {
        this.client = client;
        this.options = options;
        this.statementFactory = statementFactory;
    }

    public async Task<long> DeleteClusterAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.DeleteAsync($"{options.Prefix}cluster/{key}", cancellationToken: cancellationToken);
        return r.Deleted;
    }

    public async Task<long> DeleteListenAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.DeleteAsync($"{options.Prefix}listen/{key}", cancellationToken: cancellationToken);
        return r.Deleted;
    }

    public async Task<long> DeleteRouteAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.DeleteAsync($"{options.Prefix}route/{key}", cancellationToken: cancellationToken);
        return r.Deleted;
    }

    public async Task<long> DeleteSniAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.DeleteAsync($"{options.Prefix}sni/{key}", cancellationToken: cancellationToken);
        return r.Deleted;
    }

    public async Task<bool> ExistsClusterAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}cluster/{key}")
        }, cancellationToken: cancellationToken);
        return r.Count > 0;
    }

    public async Task<bool> ExistsListenAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}listen/{key}")
        }, cancellationToken: cancellationToken);
        return r.Count > 0;
    }

    public async Task<bool> ExistsRouteAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}route/{key}")
        }, cancellationToken: cancellationToken);
        return r.Count > 0;
    }

    public async Task<bool> ExistsSniAsync(string key, CancellationToken cancellationToken)
    {
        var r = await client.RangeAsync(new Etcdserverpb.RangeRequest()
        {
            CountOnly = true,
            Key = ByteString.CopyFromUtf8($"{options.Prefix}sni/{key}")
        }, cancellationToken: cancellationToken);
        return r.Count > 0;
    }

    public async Task<IEnumerable<ClusterConfig>> GetClusterAsync(string? prefix, CancellationToken cancellationToken)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}cluster/{prefix}", cancellationToken: cancellationToken);
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<ClusterConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 8);
            return r;
        });
    }

    public async Task<IEnumerable<ListenConfig>> GetListenAsync(string prefix, CancellationToken cancellationToken)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}listen/{prefix}", cancellationToken: cancellationToken);
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<ListenConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 7);
            return r;
        });
    }

    public async Task<IEnumerable<RouteConfig>> GetRouteAsync(string? prefix, CancellationToken cancellationToken)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}route/{prefix}", cancellationToken: cancellationToken);
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<RouteConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 6);
            return r;
        });
    }

    public async Task<IEnumerable<SniConfig>> GetSniAsync(string? prefix, CancellationToken cancellationToken)
    {
        var res = await client.GetRangeAsync($"{options.Prefix}sni/{prefix}", cancellationToken: cancellationToken);
        return res.Kvs.Select(i =>
        {
            var r = JsonSerializer.Deserialize<SniConfig>(i.Value.Span);
            r.Key = i.Key.ToStrUtf8().Substring(options.Prefix.Length + 4);
            return r;
        });
    }

    public async Task UpdateClusterAsync(ClusterConfig config, CancellationToken cancellationToken)
    {
        await client.PutAsync($"{options.Prefix}cluster/{config.Key}", JsonSerializer.Serialize(config), cancellationToken: cancellationToken);
    }

    public async Task UpdateListenAsync(ListenConfig config, CancellationToken cancellationToken)
    {
        await client.PutAsync($"{options.Prefix}listen/{config.Key}", JsonSerializer.Serialize(config), cancellationToken: cancellationToken);
    }

    public async Task UpdateRouteAsync(RouteConfig config, CancellationToken cancellationToken)
    {
        if (config.Match != null && config.Match.Statement != null)
        {
            try
            {
                statementFactory.ConvertToFunction(config.Match.Statement);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, "Statement");
            }
        }
        await client.PutAsync($"{options.Prefix}route/{config.Key}", JsonSerializer.Serialize(config), cancellationToken: cancellationToken);
    }

    public async Task UpdateSniAsync(SniConfig config, CancellationToken cancellationToken)
    {
        await client.PutAsync($"{options.Prefix}sni/{config.Key}", JsonSerializer.Serialize(config), cancellationToken: cancellationToken);
    }
}