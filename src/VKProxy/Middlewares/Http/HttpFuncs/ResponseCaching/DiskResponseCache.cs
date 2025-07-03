using Microsoft.Extensions.Caching.Distributed;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public class DiskResponseCache : IResponseCache
{
    private readonly IDiskCache cache;

    public string Name => "Disk";

    public DiskResponseCache(IDiskCache cache)
    {
        this.cache = cache;
    }

    public async ValueTask<CachedResponse?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var stream = await cache.GetStreamAsync(key, cancellationToken);
        if (stream == null) return null;
        return ResponseCacheFormatter.Deserialize(stream);
        //using var writer = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        //if (await cache.GetAsync(key, writer, cancellationToken))
        //{
        //    return ResponseCacheFormatter.Deserialize(writer.WrittenMemory);
        //}
        //return null;
    }

    public async ValueTask SetAsync(string key, CachedResponse entry, TimeSpan validFor, CancellationToken cancellationToken)
    {
        await cache.SetAsync(key, entry.Body == null ? 0 : entry.Body.Length, stream => ResponseCacheFormatter.SerializeAsync(stream, entry, cancellationToken), new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = validFor }, cancellationToken).ConfigureAwait(false);
        //using var writer = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        //ResponseCacheFormatter.Serialize(writer, entry);
        //await cache.SetAsync(key, writer.WrittenMemory, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = validFor }, cancellationToken).ConfigureAwait(false);
    }
}

public class MemoryAndDiskResponseCache : IResponseCache
{
    private readonly DiskResponseCache disk;
    private readonly MemoryResponseCache memory;

    public string Name => "MemoryAndDisk";

    public MemoryAndDiskResponseCache(DiskResponseCache disk, MemoryResponseCache memory)
    {
        this.disk = disk;
        this.memory = memory;
    }

    public async ValueTask<CachedResponse?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var r = await memory.GetAsync(key, cancellationToken);
        if (r == null)
            r = await disk.GetAsync(key, cancellationToken);
        return r;
    }

    public async ValueTask SetAsync(string key, CachedResponse entry, TimeSpan validFor, CancellationToken cancellationToken)
    {
        await disk.SetAsync(key, entry, validFor, cancellationToken);
        await memory.SetAsync(key, entry, validFor, cancellationToken);
    }
}