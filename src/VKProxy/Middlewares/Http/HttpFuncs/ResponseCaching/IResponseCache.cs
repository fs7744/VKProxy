namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public interface IResponseCache
{
    string Name { get; }

    IResponseCacheEntry? Get(string key);

    void Set(string key, IResponseCacheEntry entry, TimeSpan validFor);
}