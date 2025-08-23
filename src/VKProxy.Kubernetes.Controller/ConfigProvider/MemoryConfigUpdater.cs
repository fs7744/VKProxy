//using Microsoft.Extensions.Primitives;
//using VKProxy.Config;

//namespace VKProxy.Kubernetes.Controller.ConfigProvider;

//internal class MemoryConfigUpdater : IConfigSource<IProxyConfig>, IUpdateConfig
//{
//    private CancellationTokenSource cts;
//    private IProxyConfig snapshot;
//    public IProxyConfig CurrentSnapshot => snapshot;

//    public MemoryConfigUpdater()
//    {
//        cts = new CancellationTokenSource();
//    }

//    public void Dispose()
//    {
//        snapshot = null;
//        cts?.Dispose();
//    }

//    public Task<(IEnumerable<ListenEndPointOptions> stop, IEnumerable<ListenEndPointOptions> start)> GenerateDiffAsync(CancellationToken cancellationToken)
//    {
//        return Task.FromResult<(IEnumerable<ListenEndPointOptions> stop, IEnumerable<ListenEndPointOptions> start)>((null, null));
//    }

//    public IChangeToken? GetChangeToken()
//    {
//        return cts == null ? null : new CancellationChangeToken(cts.Token);
//    }

//    public Task UpdateAsync(IProxyConfig config, CancellationToken cancellationToken)
//    {
//        snapshot = config;
//        var oldToken = cts;
//        cts = new CancellationTokenSource();
//        oldToken?.Cancel(throwOnFirstException: false);
//        return Task.CompletedTask;
//    }
//}