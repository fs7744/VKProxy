using Microsoft.Extensions.Primitives;
using VKProxy.ServiceDiscovery;

namespace ProxyDemo.IDestinationResolvers;

public class NonStaticDNS : DestinationResolverBase // 基于 DestinationResolverBase 可以简化重复编码，cluster destinationConfigs 都会被放在 FuncDestinationResolverState 中持久
{
    public override int Order => -2;

    public override async Task ResolveAsync(FuncDestinationResolverState state, CancellationToken cancellationToken)
    {
        var tasks = state.Configs.Select(async i => await StaticDNS.QueryDNSAsync(state.Cluster, i, cancellationToken));
        await Task.WhenAll(tasks);
        state.Destinations = tasks.SelectMany(i => i.Result).ToArray(); // 变更只需要赋值替换就好， 您还可以加入变更检查，在数据未变化时减少替换的影响

        // 这里用简单的 CancellationChangeToken 延迟触发变更
        var cts = new CancellationTokenSource();
        cts.CancelAfter(60000);

        new CancellationChangeToken(cts.Token).RegisterChangeCallback(o =>
        {
            if (o is FuncDestinationResolverState s)
            {
                // 只需再次调用 ResolveAsync
                ResolveAsync(s, new CancellationTokenSource(60000).Token).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }, state);
    }
}