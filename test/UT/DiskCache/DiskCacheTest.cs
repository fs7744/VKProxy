using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using VKProxy.Core.Infrastructure;

namespace UT;

public class DiskCacheTest : IDisposable
{
    private readonly IDiskCache cache;

    public DiskCacheTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDiskCache();
        this.cache = services.BuildServiceProvider().GetRequiredService<IDiskCache>();
    }

    public void Dispose()
    {
        cache.Dispose();
    }

    [Theory]
    [InlineData("Path = '/testp'")]
    public void EqualSaveAndGet(string test)
    {
        cache.Set(test, Encoding.UTF8.GetBytes(test), new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
        });

        Assert.Equal(test, cache.GetString(test));
    }
}