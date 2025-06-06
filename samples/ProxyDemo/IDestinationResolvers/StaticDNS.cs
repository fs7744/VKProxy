using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using System.Net;
using VKProxy.Config;
using VKProxy.ServiceDiscovery;

namespace ProxyDemo.IDestinationResolvers;

public class StaticDNS : IDestinationResolver
{
    public int Order => -1;// 确保比默认dns 优先级高

    public static async Task<IEnumerable<DestinationState>> QueryDNSAsync(ClusterConfig cluster, DestinationConfig destinationConfig, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 由于address 是 uri 格式，所以需要解析 host
        var originalUri = new Uri(destinationConfig.Address);
        var originalHost = destinationConfig.Host is { Length: > 0 } host ? host : originalUri.Authority;
        var hostName = originalUri.DnsSafeHost;

        // dns query
        var addresses = await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false);

        // 修改 uri
        var uriBuilder = new UriBuilder(originalUri);
        return addresses.Select(i =>
        {
            // 修改 host 为 ip
            var addressString = i.ToString();
            uriBuilder.Host = addressString;

            return new DestinationState()
            {
                EndPoint = new IPEndPoint(i, originalUri.Port), // 设置ip ，提供给 非http 场景使用
                ClusterConfig = cluster,  // cluster 涉及健康检查配置，所以需要赋值
                Host = originalHost,  // 设置原始host，避免 http 访问失败
                Address = uriBuilder.Uri.ToString() // 设置修改后的 uri
            };
        });
    }

    public async Task<IDestinationResolverState> ResolveDestinationsAsync(ClusterConfig cluster, List<DestinationConfig> destinationConfigs, CancellationToken cancellationToken)
    {
        var tasks = destinationConfigs.Select(async i => await QueryDNSAsync(cluster, i, cancellationToken));
        await Task.WhenAll(tasks);

        return new StaticDestinationResolverState(tasks.SelectMany(i => i.Result).ToArray());
    }
}