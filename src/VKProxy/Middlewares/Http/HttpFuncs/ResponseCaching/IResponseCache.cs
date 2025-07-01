namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public interface IResponseCache
{
    string Name { get; }

    ValueTask<IResponseCacheEntry?> GetAsync(string key, CancellationToken cancellationToken);

    ValueTask SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor, CancellationToken cancellationToken);
}