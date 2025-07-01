namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public interface IResponseCache
{
    string Name { get; }

    ValueTask<IResponseCacheEntry?> GetAsync(string key);

    ValueTask SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor);
}