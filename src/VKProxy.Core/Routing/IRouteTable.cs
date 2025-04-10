namespace VKProxy.Core.Routing;

public interface IRouteTable<T> : IAsyncDisposable, IDisposable
{
    ValueTask<T> MatchAsync<R>(string key, R data, Func<T, R, bool> match);
}