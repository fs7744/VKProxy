using Microsoft.Extensions.Primitives;

namespace VKProxy.Core.Config;

public interface IConfigSource<T> : IDisposable
{
    T CurrentSnapshot { get; }

    IChangeToken? GetChangeToken();
}