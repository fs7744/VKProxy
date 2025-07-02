namespace VKProxy.Core.Infrastructure;

public class DiskCacheOptions
{
    public string Path { get; set; } = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "cache");

    public long SizeLimmit { get; set; } = 1 * 1024 * 1024 * 1024;
}
