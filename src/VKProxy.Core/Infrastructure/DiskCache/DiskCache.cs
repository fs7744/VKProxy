using DotNext.IO;
using DotNext.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using static DotNext.Threading.AsyncLockAcquisition;

namespace VKProxy.Core.Infrastructure;

public class DiskCache : IDiskCache
{
    private readonly string path;
    private long sizeLimmit;
    private readonly bool hasSizeLimit;
    private readonly ConcurrentDictionary<string, DiskCacheInfo> caches = new ConcurrentDictionary<string, DiskCacheInfo>(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> oldCaches = new ConcurrentQueue<string>();
    private readonly ILogger<DiskCache> logger;
    private bool disposabled;

    public DiskCache(DiskCacheOptions options, ILogger<DiskCache> logger)
    {
        if (!Directory.Exists(options.Path))
            Directory.CreateDirectory(options.Path);
        this.path = options.Path;
        this.sizeLimmit = options.SizeLimmit;
        this.hasSizeLimit = options.SizeLimmit > 0;
        DelayClear();
        this.logger = logger;
    }

    private void DelayClear()
    {
        if (disposabled) return;
        Task.Factory.StartNew(async () =>
        {
            if (disposabled) return;
            await Scheduler.ScheduleAsync(ClearAsync, "clear", TimeSpan.FromMinutes(1));
        });
    }

    private async ValueTask ClearAsync(string k, CancellationToken token)
    {
        foreach (var (key, info) in caches.ToArray())
        {
            if (info.Expire < DateTime.UtcNow)
            {
                try
                {
                    await RemoveAsync(key, token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
        }

        while (oldCaches.TryDequeue(out var f))
        {
            try
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        if (disposabled) return;
        DelayClear();
    }

    public byte[]? Get(string key)
    {
        return GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        if (!caches.TryGetValue(key, out var info)) return null;
        var path = info.Path;
        var expire = info.Expire;
        if (expire < DateTime.UtcNow)
        {
            await RemoveAsync(key, token);
            return null;
        }
        if (File.Exists(path))
            return await File.ReadAllBytesAsync(path, token).ConfigureAwait(false);
        else return null;
    }

    public void Refresh(string key)
    {
        RefreshAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        if (!caches.TryGetValue(key, out var info)) return;
        using (await AcquireWriteLockAsync(info.Lock, token))
        {
            if (info.Options.SlidingExpiration.HasValue)
                info.Expire = DateTime.UtcNow.Add(info.Options.SlidingExpiration.Value);
        }
    }

    public void Remove(string key)
    {
        RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        if (!caches.TryRemove(key, out var info)) return;
        using (await AcquireWriteLockAsync(info.Lock, token))
        {
            var old = info.Path;
            if (old != null)
            {
                oldCaches.Enqueue(old);
                if (hasSizeLimit)
                {
                    Interlocked.Add(ref sizeLimmit, info.Size);
                }
            }
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        SetAsync(key, value, options).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        return SetAsync(key, value.AsMemory(), options, token);
    }

    private static DiskCacheInfo NewDiskCacheInfo(string key)
    {
        return new DiskCacheInfo();
    }

    public void Dispose()
    {
        if (disposabled) return;
        disposabled = true;
        foreach (var info in caches.Values)
        {
            if (!string.IsNullOrWhiteSpace(info.Path))
                oldCaches.Enqueue(info.Path);
        }

        while (oldCaches.TryDequeue(out var f))
        {
            try
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
    }

    public async Task<bool> GetAsync(string key, IBufferWriter<byte> writer, CancellationToken cancellationToken)
    {
        if (!caches.TryGetValue(key, out var info)) return false;
        var path = info.Path;
        var expire = info.Expire;
        if (expire < DateTime.UtcNow)
        {
            return false;
        }
        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            await stream.CopyToAsync(writer.AsStream(), cancellationToken);
            return true;
        }
        else return false;
    }

    public async Task<Stream?> GetStreamAsync(string key, CancellationToken cancellationToken)
    {
        if (!caches.TryGetValue(key, out var info)) return null;
        var path = info.Path;
        var expire = info.Expire;
        if (expire < DateTime.UtcNow)
        {
            return null;
        }
        if (File.Exists(path))
        {
            return File.OpenRead(path);
        }
        else return null;
    }

    public async Task SetAsync(string key, long size, Func<Stream, Task> func, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
    {
        var info = caches.GetOrAdd(key, NewDiskCacheInfo);
        using (await AcquireWriteLockAsync(info.Lock, cancellationToken))
        {
            var old = info.Path;
            var oldSize = info.Size;
            var newSize = size;
            var change = newSize - oldSize;
            if (!hasSizeLimit || Interlocked.Add(ref sizeLimmit, change * -1) >= 0)
            {
                var newPath = Path.Combine(path, Guid.NewGuid().ToString());
                using var stream = File.OpenWrite(newPath);
                await func(stream).ConfigureAwait(false);
                info.Options = options;
                info.Path = newPath;
                info.Size = newSize;
                if (options.AbsoluteExpiration.HasValue)
                    info.Expire = options.AbsoluteExpiration.Value.DateTime.ToUniversalTime();
                else if (options.AbsoluteExpirationRelativeToNow.HasValue)
                    info.Expire = DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
                else if (options.SlidingExpiration.HasValue)
                    info.Expire = DateTime.UtcNow.Add(options.SlidingExpiration.Value);
                else
                    info.Expire = DateTime.UtcNow;
            }
            if (old != null)
            {
                oldCaches.Enqueue(old);
                if (hasSizeLimit)
                {
                    Interlocked.Add(ref sizeLimmit, oldSize);
                }
            }
        }
    }

    public async Task SetAsync(string key, ReadOnlyMemory<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
    {
        var info = caches.GetOrAdd(key, NewDiskCacheInfo);
        using (await AcquireWriteLockAsync(info.Lock, cancellationToken))
        {
            var old = info.Path;
            var oldSize = info.Size;
            var newSize = value.Length;
            var change = newSize - oldSize;
            if (!hasSizeLimit || Interlocked.Add(ref sizeLimmit, change * -1) >= 0)
            {
                var newPath = Path.Combine(path, Guid.NewGuid().ToString());
                await File.WriteAllBytesAsync(newPath, value, cancellationToken).ConfigureAwait(false);
                info.Options = options;
                info.Path = newPath;
                info.Size = newSize;
                if (options.AbsoluteExpiration.HasValue)
                    info.Expire = options.AbsoluteExpiration.Value.DateTime.ToUniversalTime();
                else if (options.AbsoluteExpirationRelativeToNow.HasValue)
                    info.Expire = DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
                else if (options.SlidingExpiration.HasValue)
                    info.Expire = DateTime.UtcNow.Add(options.SlidingExpiration.Value);
                else
                    info.Expire = DateTime.UtcNow;
            }
            if (old != null)
            {
                oldCaches.Enqueue(old);
                if (hasSizeLimit)
                {
                    Interlocked.Add(ref sizeLimmit, oldSize);
                }
            }
        }
    }
}