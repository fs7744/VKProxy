using DotNext.Threading;
using Microsoft.Extensions.Caching.Distributed;

namespace VKProxy.Core.Infrastructure;

internal class DiskCacheInfo
{
    public AsyncReaderWriterLock Lock = new();
    public string? Path;
    public DistributedCacheEntryOptions Options;
    public long Size;
    public DateTime Expire;
}