using Microsoft.Extensions.Caching.Distributed;
using System.Buffers;

namespace VKProxy.Core.Infrastructure;

public interface IDiskCache : IDistributedCache, IDisposable
{
    Task<bool> GetAsync(string key, IBufferWriter<byte> writer, CancellationToken cancellationToken);

    Task SetAsync(string key, ReadOnlyMemory<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken);

    Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken);

    Task SetAsync(string key, long size, Func<Stream, Task> func, DistributedCacheEntryOptions options, CancellationToken cancellationToken);
}