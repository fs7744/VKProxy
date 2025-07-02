namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public interface IResponseCache
{
    string Name { get; }

    ValueTask<CachedResponse?> GetAsync(string key, CancellationToken cancellationToken);

    ValueTask SetAsync(string key, CachedResponse entry, TimeSpan validFor, CancellationToken cancellationToken);
}