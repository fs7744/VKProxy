using Microsoft.Extensions.Primitives;

namespace VKProxy.Config;

public interface IConfigSource<T> : IDisposable
{
    T CurrentSnapshot { get; }

    Task<(IEnumerable<ListenEndPointOptions> stop, IEnumerable<ListenEndPointOptions> start)> GenerateDiffAsync(CancellationToken cancellationToken);

    IChangeToken? GetChangeToken();
}