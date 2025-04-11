using Microsoft.Extensions.Primitives;

namespace VKProxy.Core.Config;

public interface IConfigSource<T>
{
    T CurrentSnapshot { get; }

    IChangeToken? GetChangeToken();
}