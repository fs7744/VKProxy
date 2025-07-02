using Microsoft.Extensions.Caching.Distributed;
using System.Buffers;

namespace VKProxy.Core.Infrastructure;

public interface IDiskCache : IDistributedCache, IDisposable
{
    Task<bool> GetAsync(string key, IBufferWriter<byte> writer, CancellationToken cancellationToken);

    Task SetAsync(string key, ReadOnlyMemory<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken);
}