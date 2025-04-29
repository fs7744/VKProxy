using dotnet_etcd.interfaces;
using Microsoft.Extensions.Primitives;
using VKProxy.Config;

namespace VKProxy.Storages.Etcd;

internal class EtcdProxyConfigSource : IConfigSource<IProxyConfig>
{
    private readonly IEtcdClient client;
    private readonly EtcdProxyConfigSourceOptions options;

    public IProxyConfig CurrentSnapshot => throw new NotImplementedException();

    internal EtcdProxyConfigSource(IEtcdClient client, EtcdProxyConfigSourceOptions options)
    {
        this.client = client;
        this.options = options;
    }

    public void Dispose()
    {
        client?.Dispose();
    }

    public Task<(IEnumerable<ListenEndPointOptions> stop, IEnumerable<ListenEndPointOptions> start)> GenerateDiffAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IChangeToken? GetChangeToken()
    {
        throw new NotImplementedException();
    }
}