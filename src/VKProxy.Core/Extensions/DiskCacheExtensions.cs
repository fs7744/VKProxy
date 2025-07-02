using Microsoft.Extensions.Caching.Distributed;
using VKProxy.Core.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class DiskCacheExtensions
{
    public static IServiceCollection AddDiskCache(this IServiceCollection services, bool toDistributedCache = false, Action<DiskCacheOptions> action = null)
    {
        var op = new DiskCacheOptions();
        action?.Invoke(op);
        services.AddSingleton<DiskCacheOptions>(op);
        services.AddSingleton<IDiskCache, DiskCache>();
        if (toDistributedCache)
            services.AddSingleton<IDistributedCache>(i => i.GetRequiredService<IDiskCache>());

        return services;
    }
}