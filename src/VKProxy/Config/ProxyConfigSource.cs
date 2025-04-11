using Microsoft.Extensions.Primitives;
using VKProxy.Core.Config;

namespace VKProxy.Config;

internal class ProxyConfigSource : IConfigSource<IProxyConfig>
{
    public IProxyConfig CurrentSnapshot { get; private set; }

    public IChangeToken? GetChangeToken()
    {
        throw new NotImplementedException();
    }
}